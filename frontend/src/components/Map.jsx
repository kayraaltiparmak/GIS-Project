import { useEffect, useRef, useState } from "react";
import "ol/ol.css";
import OLMap from "ol/Map";
import Feature from "ol/Feature";
import PointGeometry from "ol/geom/Point";
import View from "ol/View";
import TileLayer from "ol/layer/Tile";
import VectorLayer from "ol/layer/Vector";
import HeatmapLayer from "ol/layer/Heatmap";
import OSM from "ol/source/OSM";
import TileWMS from "ol/source/TileWMS";
import VectorSource from "ol/source/Vector";
import Collection from "ol/Collection";
import Draw from "ol/interaction/Draw";
import Modify from "ol/interaction/Modify";
import Translate from "ol/interaction/Translate";
import WKT from "ol/format/WKT";
import GeoJSON from "ol/format/GeoJSON";
import { fromLonLat } from "ol/proj";
import Style from "ol/style/Style";
import Stroke from "ol/style/Stroke";
import Fill from "ol/style/Fill";
import CircleStyle from "ol/style/Circle";
import Icon from "ol/style/Icon";
import { unByKey } from "ol/Observable";
import Navbar from "./Navbar";
import {
  GEOSERVER_DEBUG,
  GEOSERVER_HEATMAP_STYLE,
  GEOSERVER_LAYERS,
  GEOSERVER_POI_STYLE,
  GEOSERVER_LOCATION_ANALYSIS_LAYER,
  GEOSERVER_LOCATION_ANALYSIS_STYLE,
  GEOSERVER_WMS_URL
} from "../config/geoserver";
import { buildLayerCqlFilter, buildUserCqlFilter, getFeaturesFromLayers } from "../services/geoserverWfs";

const TOKEN_KEY = "token";
const TURKEY_CENTER = [35.2433, 38.9637];
const API_URL = "http://localhost:5291/api";
const DEFAULT_COLOR = "#1d4ed8";
const COLOR_PALETTE = [
  "#2563eb",
  "#06b6d4",
  "#14b8a6",
  "#22c55e",
  "#f59e0b",
  "#ef4444",
  "#ec4899",
  "#8b5cf6",
  "#475569",
  "#111827"
];
const CUSTOM_COLOR_PALETTE = [
  "#fee2e2",
  "#ffedd5",
  "#fef3c7",
  "#dcfce7",
  "#ccfbf1",
  "#dbeafe",
  "#ede9fe",
  "#fce7f3",
  "#fca5a5",
  "#fdba74",
  "#fde047",
  "#86efac",
  "#5eead4",
  "#93c5fd",
  "#c4b5fd",
  "#f9a8d4"
];

const drawSettings = {
  Poi: { drawType: "Point", endpoint: "Pois" },
  Point: { drawType: "Point", endpoint: "TblPoint" },
  Line: { drawType: "LineString", endpoint: "TblLine" },
  Polygon: { drawType: "Polygon", endpoint: "TblPolygon" },
  InventoryAnalysis: { drawType: "Polygon", endpoint: "InventoryAnalysis" }
  ,LocationAnalysis: { drawType: "Polygon", endpoint: "LocationAnalysis" }
};

const WMS_Z_INDEX = {
  Polygon: 10,
  Line: 20,
  Point: 30,
  Poi: 35,
  Selection: 40,
  Heatmap: 70,
  Analysis: 80
};

function formatTime(totalSeconds) {
  const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return minutes + ":" + seconds;
}

function transparentColor(hex) {
  const value = hex.replace("#", "");
  const red = parseInt(value.slice(0, 2), 16);
  const green = parseInt(value.slice(2, 4), 16);
  const blue = parseInt(value.slice(4, 6), 16);

  return "rgba(" + red + ", " + green + ", " + blue + ", 0.22)";
}

function createFeatureStyle(color) {
  return new Style({
    image: new CircleStyle({
      radius: 7,
      fill: new Fill({ color }),
      stroke: new Stroke({ color: "#ffffff", width: 2 })
    }),
    stroke: new Stroke({ color, width: 3 }),
    fill: new Fill({ color: transparentColor(color) })
  });
}

function createPoiIconStyle(iconKey = "default") {
  return new Style({ image: new Icon({ src: "/poi-icons/" + iconKey + ".svg", anchor: [0.5, 1], scale: 0.9 }) });
}

function createAnalysisStyle() {
  return new Style({
    stroke: new Stroke({ color: "#0f766e", width: 3, lineDash: [8, 6] }),
    fill: new Fill({ color: "rgba(15, 118, 110, 0.16)" })
  });
}

function hexToHsl(hex) {
  const value = hex.replace("#", "");
  const red = parseInt(value.slice(0, 2), 16) / 255;
  const green = parseInt(value.slice(2, 4), 16) / 255;
  const blue = parseInt(value.slice(4, 6), 16) / 255;
  const max = Math.max(red, green, blue);
  const min = Math.min(red, green, blue);
  const lightness = (max + min) / 2;

  if (max === min) {
    return { hue: 0, saturation: 0, lightness: Math.round(lightness * 100) };
  }

  const delta = max - min;
  const saturation = lightness > 0.5 ? delta / (2 - max - min) : delta / (max + min);
  let hue;

  if (max === red) {
    hue = (green - blue) / delta + (green < blue ? 6 : 0);
  } else if (max === green) {
    hue = (blue - red) / delta + 2;
  } else {
    hue = (red - green) / delta + 4;
  }

  return {
    hue: Math.round(hue * 60),
    saturation: Math.round(saturation * 100),
    lightness: Math.round(lightness * 100)
  };
}

function hslToHex(hue, saturation, lightness) {
  const normalizedSaturation = saturation / 100;
  const normalizedLightness = lightness / 100;
  const chroma = (1 - Math.abs(2 * normalizedLightness - 1)) * normalizedSaturation;
  const hueSegment = hue / 60;
  const secondComponent = chroma * (1 - Math.abs((hueSegment % 2) - 1));
  const match = normalizedLightness - chroma / 2;
  let red = 0;
  let green = 0;
  let blue = 0;

  if (hueSegment >= 0 && hueSegment < 1) {
    red = chroma;
    green = secondComponent;
  } else if (hueSegment >= 1 && hueSegment < 2) {
    red = secondComponent;
    green = chroma;
  } else if (hueSegment >= 2 && hueSegment < 3) {
    green = chroma;
    blue = secondComponent;
  } else if (hueSegment >= 3 && hueSegment < 4) {
    green = secondComponent;
    blue = chroma;
  } else if (hueSegment >= 4 && hueSegment < 5) {
    red = secondComponent;
    blue = chroma;
  } else {
    red = chroma;
    blue = secondComponent;
  }

  return [red, green, blue]
    .map((component) => Math.round((component + match) * 255).toString(16).padStart(2, "0"))
    .join("")
    .replace(/^/, "#");
}

function ColorPickerField({ value, onChange, disabled = false }) {
  const [isOpen, setIsOpen] = useState(false);
  const normalizedValue = value.toLowerCase();
  const hslValue = hexToHsl(value);

  function selectColor(color) {
    onChange(color);
    setIsOpen(false);
  }

  function handleHexChange(event) {
    const nextValue = event.target.value;

    if (/^#[0-9a-fA-F]{6}$/.test(nextValue)) {
      onChange(nextValue);
    }
  }

  function handleHslChange(name, nextValue) {
    const nextHsl = {
      ...hslValue,
      [name]: Number(nextValue)
    };

    onChange(hslToHex(nextHsl.hue, nextHsl.saturation, nextHsl.lightness));
  }

  return (
    <div className="color-picker-field">
      <button
        type="button"
        className="color-field-trigger"
        onClick={() => setIsOpen((current) => !current)}
        disabled={disabled}
      >
        <span className="selected-color-preview" style={{ background: value }} />
        <span>{value}</span>
      </button>

      {isOpen && !disabled && (
        <div className="color-popover">
          <div className="color-popover-section">
            <span>Temel renkler</span>
            <div className="color-swatch-grid" aria-label="Temel renkler">
              {COLOR_PALETTE.map((color) => (
                <button
                  type="button"
                  key={color}
                  className={normalizedValue === color ? "color-swatch selected-swatch" : "color-swatch"}
                  style={{ background: color }}
                  onClick={() => selectColor(color)}
                  aria-label={color + " rengini sec"}
                />
              ))}
            </div>
          </div>

          <div className="color-popover-section">
            <span>Ozel renkler</span>
            <div className="color-swatch-grid custom-swatch-grid" aria-label="Ozel renkler">
              {CUSTOM_COLOR_PALETTE.map((color) => (
                <button
                  type="button"
                  key={color}
                  className={normalizedValue === color ? "color-swatch selected-swatch" : "color-swatch"}
                  style={{ background: color }}
                  onClick={() => selectColor(color)}
                  aria-label={color + " rengini sec"}
                />
              ))}
            </div>
          </div>

          <div className="color-adjust-panel">
            <div className="color-adjust-preview">
              <span style={{ background: value }} />
              <strong>{value}</strong>
            </div>

            <label>
              <span>Ton</span>
              <input
                type="range"
                min="0"
                max="359"
                value={hslValue.hue}
                onChange={(event) => handleHslChange("hue", event.target.value)}
              />
              <strong>{hslValue.hue}</strong>
            </label>

            <label>
              <span>Doygun</span>
              <input
                type="range"
                min="0"
                max="100"
                value={hslValue.saturation}
                onChange={(event) => handleHslChange("saturation", event.target.value)}
              />
              <strong>{hslValue.saturation}</strong>
            </label>

            <label>
              <span>Parlak</span>
              <input
                type="range"
                min="0"
                max="100"
                value={hslValue.lightness}
                onChange={(event) => handleHslChange("lightness", event.target.value)}
              />
              <strong>{hslValue.lightness}</strong>
            </label>
          </div>

          <div className="color-hex-row">
            <input
              type="text"
              key={value}
              defaultValue={value}
              onChange={handleHexChange}
              maxLength="7"
              aria-label="Hex renk kodu"
            />
            <button type="button" onClick={() => setIsOpen(false)}>Tamam</button>
          </div>
        </div>
      )}
    </div>
  );
}

function Map({ remainingSeconds, userInfo, onAdminOpen, onLogout }) {
  const mapElementRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const drawInteractionRef = useRef(null);
  const editInteractionsRef = useRef([]);
  const originalDetailGeometryRef = useRef(null);
  const vectorSourcesRef = useRef({
    Poi: new VectorSource(),
    Point: new VectorSource(),
    Line: new VectorSource(),
    Polygon: new VectorSource()
  });
  const analysisSourceRef = useRef(new VectorSource());
  const vectorLayersRef = useRef({});
  const wmsLayersRef = useRef({});
  const heatmapLayerRef = useRef(null);
  const heatmapSourceRef = useRef(new VectorSource());
  const heatmapVectorLayerRef = useRef(null);
  const analysisLayerRef = useRef(null);
  const locationHeatmapLayerRef = useRef(null);
  const locationHeatmapSourceRef = useRef(new VectorSource());
  const locationHeatmapVectorLayerRef = useRef(null);
  const geoJsonFormatRef = useRef(new GeoJSON());
  const wktFormatRef = useRef(new WKT());
  const onLogoutRef = useRef(onLogout);
  const userInfoRef = useRef(userInfo);
  const layerVisibilityRef = useRef(null);
  const activeToolRef = useRef(null);
  const pendingFeatureRef = useRef(null);
  const isDetailEditingRef = useRef(false);
  const [activeTool, setActiveTool] = useState(null);
  const [pendingFeature, setPendingFeature] = useState(null);
  const [selectedFeature, setSelectedFeature] = useState(null);
  const [selectionFeatures, setSelectionFeatures] = useState([]);
  const [isHeatmapActive, setIsHeatmapActive] = useState(false);
  const [layerVisibility, setLayerVisibility] = useState({
    Poi: true,
    Point: true,
    Line: true,
    Polygon: true
  });
  const [attributeForm, setAttributeForm] = useState({ name: "", color: DEFAULT_COLOR });
  const [poiCategories, setPoiCategories] = useState([]);
  const [detailForm, setDetailForm] = useState({ name: "", color: DEFAULT_COLOR });
  const [isDetailEditing, setIsDetailEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDetailSaving, setIsDetailSaving] = useState(false);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [popupError, setPopupError] = useState("");
  const [detailError, setDetailError] = useState("");
  const [inventoryMessage, setInventoryMessage] = useState("");
  const [analysisMessage, setAnalysisMessage] = useState("");
  const [heatmapMessage, setHeatmapMessage] = useState("");
  const [queryFilters, setQueryFilters] = useState({
    type: "",
    name: "",
    sortBy: "modifiedDate",
    sortDirection: "desc"
  });
  const [queryResults, setQueryResults] = useState([]);
  const [queryError, setQueryError] = useState("");
  const [queryReloadKey, setQueryReloadKey] = useState(0);
  const [poiSearch, setPoiSearch] = useState("");
  const [poiSearchResults, setPoiSearchResults] = useState([]);
  const [poiSearchError, setPoiSearchError] = useState("");
  const [locationPanelOpen, setLocationPanelOpen] = useState(false);
  const [targetGeometry, setTargetGeometry] = useState(null);
  const [criteria, setCriteria] = useState([{ categoryId: "", weight: "" }, { categoryId: "", weight: "" }]);
  const [locationResult, setLocationResult] = useState(null);
  const [locationError, setLocationError] = useState("");
  const [locationLoading, setLocationLoading] = useState(false);
  const [targetMethod, setTargetMethod] = useState("draw");
  const [provinces, setProvinces] = useState([]);
  const [selectedProvince, setSelectedProvince] = useState("");

  useEffect(() => {
    onLogoutRef.current = onLogout;
  }, [onLogout]);

  useEffect(() => {
    userInfoRef.current = userInfo;
  }, [userInfo]);

  useEffect(() => {
    if (!(userInfo?.permissions ?? []).includes("Poi.Search") || poiSearch.trim().length < 2) { setPoiSearchResults([]); setPoiSearchError(""); return; }
    const controller = new AbortController();
    const timer = window.setTimeout(async () => {
      try {
        const response = await fetch(API_URL + "/poi/search?q=" + encodeURIComponent(poiSearch.trim()) + "&limit=10", { signal: controller.signal, headers: { Authorization: "Bearer " + localStorage.getItem(TOKEN_KEY) } });
        if (!response.ok) throw new Error("Arama yapılamadı.");
        setPoiSearchResults(await response.json()); setPoiSearchError("");
      } catch (error) { if (error.name !== "AbortError") setPoiSearchError(error.message); }
    }, 350);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [poiSearch, userInfo]);

  function selectPoiSearchResult(item) {
    const feature = new Feature({ geometry: new PointGeometry(fromLonLat([item.longitude, item.latitude])) });
    Object.entries({ id:item.id, endpoint:"Pois", type:"Poi", name:item.name, categoryId:item.categoryId, categoryName:item.categoryName, parentCategoryName:item.parentCategoryName, workingHours:item.workingHours }).forEach(([key,value]) => feature.set(key,value));
    feature.set("iconKey", item.iconKey || "default");
    feature.setStyle(createPoiIconStyle(item.iconKey));
    vectorSourcesRef.current.Poi.clear(); vectorSourcesRef.current.Poi.addFeature(feature);
    mapInstanceRef.current?.getView().animate({ center: feature.getGeometry().getCoordinates(), zoom: 17, duration: 650 });
    setPoiSearchResults([]); setPoiSearch(item.name); openDetailPopup(feature); loadPoiDetail(feature);
  }

  useEffect(() => {
    layerVisibilityRef.current = layerVisibility;
  }, [layerVisibility]);

  useEffect(() => {
    activeToolRef.current = activeTool;
  }, [activeTool]);

  useEffect(() => {
    pendingFeatureRef.current = pendingFeature;
  }, [pendingFeature]);

  useEffect(() => {
    isDetailEditingRef.current = isDetailEditing;
  }, [isDetailEditing]);

  useEffect(() => {
    if (!mapElementRef.current || mapInstanceRef.current) {
      return;
    }

    wmsLayersRef.current = {
      Polygon: createWmsLayer("Polygon", userInfo?.userId, layerVisibility.Polygon),
      Line: createWmsLayer("Line", userInfo?.userId, layerVisibility.Line),
      Point: createWmsLayer("Point", userInfo?.userId, layerVisibility.Point),
      Poi: createWmsLayer("Poi", userInfo?.userId, layerVisibility.Poi)
    };
    heatmapLayerRef.current = createHeatmapWmsLayer(userInfo?.userId);
    heatmapVectorLayerRef.current = new HeatmapLayer({
      source: heatmapSourceRef.current,
      blur: 20,
      radius: 28,
      weight: () => 1,
      visible: false,
      zIndex: WMS_Z_INDEX.Heatmap + 1
    });
    vectorLayersRef.current = {
      Poi: new VectorLayer({ source: vectorSourcesRef.current.Poi, zIndex: WMS_Z_INDEX.Selection + 1, style: createFeatureStyle("#dc2626") }),
      Point: new VectorLayer({
        source: vectorSourcesRef.current.Point,
        zIndex: WMS_Z_INDEX.Selection
      }),
      Line: new VectorLayer({
        source: vectorSourcesRef.current.Line,
        zIndex: WMS_Z_INDEX.Selection
      }),
      Polygon: new VectorLayer({
        source: vectorSourcesRef.current.Polygon,
        zIndex: WMS_Z_INDEX.Selection
      })
    };
    analysisLayerRef.current = new VectorLayer({
      source: analysisSourceRef.current,
      zIndex: WMS_Z_INDEX.Analysis
    });
    locationHeatmapLayerRef.current = new TileLayer({
      source: new TileWMS({ url: GEOSERVER_WMS_URL, params: { LAYERS: GEOSERVER_LOCATION_ANALYSIS_LAYER, STYLES: GEOSERVER_LOCATION_ANALYSIS_STYLE, TILED: true }, serverType: "geoserver", crossOrigin: "anonymous" }),
      visible: false, zIndex: WMS_Z_INDEX.Heatmap
    });
    locationHeatmapVectorLayerRef.current = new HeatmapLayer({
      source: locationHeatmapSourceRef.current,
      blur: 28,
      radius: 42,
      weight: feature => feature.get("heatWeight") || 0,
      gradient: ["#2563eb", "#06b6d4", "#22c55e", "#fde047", "#ef4444"],
      visible: false,
      opacity: 0.9,
      zIndex: WMS_Z_INDEX.Heatmap + 2
    });

    mapInstanceRef.current = new OLMap({
      target: mapElementRef.current,
      layers: [
        new TileLayer({
          source: new OSM()
        }),
        wmsLayersRef.current.Polygon,
        wmsLayersRef.current.Line,
        wmsLayersRef.current.Point,
        wmsLayersRef.current.Poi,
        locationHeatmapLayerRef.current,
        locationHeatmapVectorLayerRef.current,
        heatmapLayerRef.current,
        heatmapVectorLayerRef.current,
        vectorLayersRef.current.Polygon,
        vectorLayersRef.current.Line,
        vectorLayersRef.current.Point,
        vectorLayersRef.current.Poi,
        analysisLayerRef.current
      ],
      view: new View({
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6
      })
    });

    const clickKey = mapInstanceRef.current.on("singleclick", async (event) => {
      if (activeToolRef.current || pendingFeatureRef.current || isDetailEditingRef.current) {
        return;
      }

      const features = await getSelectableFeaturesAtEvent(event);

      if (features.length === 0) {
        setSelectionFeatures([]);
        closeDetailPopup();
        return;
      }

      if (features.length === 1) {
        setSelectionFeatures([]);
        openDetailPopup(features[0]);
        return;
      }

      closeDetailPopup();
      setSelectionFeatures(features);
    });

    return () => {
      if (clickKey) {
        unByKey(clickKey);
      }
      mapInstanceRef.current?.setTarget(undefined);
      mapInstanceRef.current = null;
    };
  }, []);

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token || !(userInfo?.permissions ?? []).includes("Poi.View")) return;
    fetch(API_URL + "/poi-categories/active", { headers: { Authorization: "Bearer " + token } })
      .then(r => r.ok ? r.json() : []).then(setPoiCategories)
      .catch(() => setPopupError("POI kategorileri yüklenemedi."));
  }, [userInfo]);

  useEffect(() => {
    if (!(userInfo?.permissions ?? []).includes("Poi.View")) {
      vectorSourcesRef.current.Poi.clear();
      return;
    }
    let cancelled = false;
    const token = localStorage.getItem(TOKEN_KEY);
    fetch(API_URL + "/pois", { headers: { Authorization: "Bearer " + token } })
      .then(response => {
        if (!response.ok) throw new Error("POI kayıtları alınamadı.");
        return response.json();
      })
      .then(items => {
        if (cancelled) return;
        const source = vectorSourcesRef.current.Poi;
        source.clear();
        items.forEach(item => {
          const feature = wktFormatRef.current.readFeature(item.wkt, { dataProjection: "EPSG:4326", featureProjection: "EPSG:3857" });
          Object.entries({ id:item.id, endpoint:"Pois", type:"Poi", name:item.name, categoryId:item.categoryId, categoryName:item.categoryName, parentCategoryName:item.parentCategoryName, workingHours:item.workingHours, iconKey:item.iconKey || "default", username:item.username, createdDate:item.createdDate }).forEach(([key, value]) => feature.set(key, value));
          feature.setStyle(createPoiIconStyle(item.iconKey));
          feature.set("isBackendFallback", true);
          source.addFeature(feature);
        });
      })
      .catch(() => setPopupError("POI katmanı yüklenemedi."));
    return () => { cancelled = true; };
  }, [userInfo?.userId, userInfo?.permissions, queryReloadKey]);

  useEffect(() => {
    if (!(userInfo?.permissions ?? []).includes("LocationAnalysis.Use")) return;
    fetch(API_URL + "/provinces", { headers: { Authorization: "Bearer " + localStorage.getItem(TOKEN_KEY) } })
      .then(r => r.ok ? r.json() : []).then(setProvinces).catch(() => setProvinces([]));
  }, [userInfo]);

  useEffect(() => {
    Object.entries(layerVisibility).forEach(([type, isVisible]) => {
      wmsLayersRef.current[type]?.setVisible(isVisible);
      vectorLayersRef.current[type]?.setVisible(isVisible);
    });
  }, [layerVisibility]);

  useEffect(() => {
    Object.keys(GEOSERVER_LAYERS).forEach((type) => updateWmsCqlFilter(type));
    const poiSource = wmsLayersRef.current.Poi?.getSource();
    if (poiSource) {
      poiSource.updateParams({
        CQL_FILTER: buildLayerCqlFilter("Poi"),
        _session: (userInfo?.userId ?? "anonymous") + "-" + Date.now()
      });
    }
    updateHeatmapCqlFilter();
  }, [userInfo?.userId]);

  useEffect(() => {
    let isCancelled = false;

    async function loadQueryResults() {
      const token = localStorage.getItem(TOKEN_KEY);

      if (!token) {
        onLogoutRef.current();
        return;
      }

      const params = new URLSearchParams();

      if (queryFilters.type) {
        params.set("type", queryFilters.type);
      }

      if (queryFilters.name.trim()) {
        params.set("name", queryFilters.name.trim());
      }

      params.set("sortBy", queryFilters.sortBy);
      params.set("sortDirection", queryFilters.sortDirection);

      try {
        const response = await fetch(API_URL + "/Map/geometries?" + params.toString(), {
          headers: {
            "Authorization": "Bearer " + token
          }
        });

        if (response.status === 401) {
          onLogoutRef.current();
          return;
        }

        if (!response.ok) {
          setQueryError("Sorgulama sirasinda bir sorun olustu.");
          return;
        }

        const data = await response.json();

        if (isCancelled) {
          return;
        }

        setQueryResults(data);
        syncFallbackFeatures(data);
        setQueryError("");
      } catch {
        if (!isCancelled) {
          setQueryError("Sunucuya baglanilamadi.");
        }
      }
    }

    loadQueryResults();

    return () => {
      isCancelled = true;
    };
  }, [queryFilters, queryReloadKey]);

  useEffect(() => {
    const map = mapInstanceRef.current;

    if (!map) {
      return;
    }

    if (drawInteractionRef.current) {
      map.removeInteraction(drawInteractionRef.current);
      drawInteractionRef.current = null;
    }

    if (!activeTool) {
      return;
    }

    const setting = drawSettings[activeTool];
    const draw = new Draw({
      source: activeTool === "InventoryAnalysis" || activeTool === "LocationAnalysis" ? analysisSourceRef.current : getSourceForType(activeTool),
      type: setting.drawType
    });

    draw.on("drawend", (event) => {
      if (activeTool === "InventoryAnalysis") {
        analysisSourceRef.current.clear();
        event.feature.setStyle(createAnalysisStyle());
        analysisSourceRef.current.addFeature(event.feature);
        analyzeInventory(event.feature);
        activeToolRef.current = null;
        map.removeInteraction(draw);
        drawInteractionRef.current = null;
        setActiveTool(null);
        return;
      }
      if (activeTool === "LocationAnalysis") {
        analysisSourceRef.current.clear(); event.feature.setStyle(createAnalysisStyle()); analysisSourceRef.current.addFeature(event.feature);
        setTargetGeometry(geoJsonFormatRef.current.writeGeometryObject(event.feature.getGeometry(), { featureProjection: "EPSG:3857", dataProjection: "EPSG:4326" }));
        setLocationError(""); activeToolRef.current = null; map.removeInteraction(draw); drawInteractionRef.current = null; setActiveTool(null); return;
      }

      setPendingFeature({ feature: event.feature, endpoint: setting.endpoint });
      setAttributeForm({ name: "", color: DEFAULT_COLOR, categoryId: "", workingHours: "" });
      setPopupError("");
      setInventoryMessage("");
      activeToolRef.current = null;
      map.removeInteraction(draw);
      drawInteractionRef.current = null;
      setActiveTool(null);
    });

    map.addInteraction(draw);
    drawInteractionRef.current = draw;

    return () => {
      if (drawInteractionRef.current) {
        map.removeInteraction(drawInteractionRef.current);
        drawInteractionRef.current = null;
      }
    };
  }, [activeTool]);

  useEffect(() => {
    const map = mapInstanceRef.current;

    clearEditInteractions();

    if (!map || !selectedFeature || !isDetailEditing || isDetailSaving) {
      return;
    }

    const selectedFeatures = new Collection([selectedFeature]);
    const interactions = [
      new Translate({ features: selectedFeatures })
    ];

    if (selectedFeature.get("type") !== "Point") {
      interactions.push(new Modify({ features: selectedFeatures }));
    }

    interactions.forEach((interaction) => map.addInteraction(interaction));
    editInteractionsRef.current = interactions;

    return () => {
      clearEditInteractions();
    };
  }, [selectedFeature, isDetailEditing, isDetailSaving]);

  function handleAttributeChange(event) {
    const { name, value } = event.target;
    setAttributeForm((current) => ({ ...current, [name]: value }));
  }

  function handleDetailChange(event) {
    const { name, value } = event.target;
    setDetailForm((current) => ({ ...current, [name]: value }));
  }

  function handleAttributeColorChange(color) {
    setAttributeForm((current) => ({ ...current, color }));
  }

  function handleDetailColorChange(color) {
    setDetailForm((current) => ({ ...current, color }));
  }

  function getSourceForType(type) {
    return vectorSourcesRef.current[type] ?? null;
  }

  function createWmsLayer(type, userId, isVisible) {
    const layer = GEOSERVER_LAYERS[type];
    const cqlFilter = buildLayerCqlFilter(type, userId);
    const params = {
      LAYERS: layer.name,
      TILED: true,
      VERSION: "1.1.0",
      FORMAT: "image/png",
      TRANSPARENT: true
    };
    if (type === "Poi") params.STYLES = GEOSERVER_POI_STYLE;

    if (cqlFilter) {
      params.CQL_FILTER = cqlFilter;
    }

    debugWmsParams("layer", layer.name, params);

    return new TileLayer({
      visible: isVisible,
      opacity: 1,
      zIndex: WMS_Z_INDEX[type],
      source: new TileWMS({
        url: GEOSERVER_WMS_URL,
        crossOrigin: "anonymous",
        params,
        serverType: "geoserver"
      })
    });
  }

  function createHeatmapWmsLayer(userId) {
    const cqlFilter = buildUserCqlFilter(userId);
    const params = {
      LAYERS: GEOSERVER_LAYERS.Point.name,
      STYLES: GEOSERVER_HEATMAP_STYLE,
      TILED: true,
      VERSION: "1.1.0",
      FORMAT: "image/png",
      TRANSPARENT: true
    };

    if (cqlFilter) {
      params.CQL_FILTER = cqlFilter;
    }

    debugWmsParams("heatmap", GEOSERVER_LAYERS.Point.name, params);

    return new TileLayer({
      visible: false,
      opacity: 0.85,
      zIndex: WMS_Z_INDEX.Heatmap,
      source: new TileWMS({
        url: GEOSERVER_WMS_URL,
        crossOrigin: "anonymous",
        params,
        serverType: "geoserver"
      })
    });
  }

  function debugWmsParams(kind, layerName, params) {
    if (!GEOSERVER_DEBUG) {
      return;
    }

    console.log("[GeoServer WMS]", {
      kind,
      layer: layerName,
      styles: params.STYLES || "",
      cqlFilter: params.CQL_FILTER || "",
      params,
      baseUrl: GEOSERVER_WMS_URL
    });
  }

  function updateWmsCqlFilter(type) {
    const source = wmsLayersRef.current[type]?.getSource();

    if (!source) {
      return;
    }

    const cqlFilter = buildLayerCqlFilter(type, userInfoRef.current?.userId);
    const params = cqlFilter ? { CQL_FILTER: cqlFilter } : { CQL_FILTER: null };
    source.updateParams(params);
    debugWmsParams("layer", GEOSERVER_LAYERS[type].name, source.getParams());
  }

  function updateHeatmapCqlFilter() {
    const source = heatmapLayerRef.current?.getSource();

    if (!source) {
      return;
    }

    const cqlFilter = buildUserCqlFilter(userInfoRef.current?.userId);
    const params = cqlFilter ? { CQL_FILTER: cqlFilter } : { CQL_FILTER: null };
    source.updateParams(params);
    debugWmsParams("heatmap", GEOSERVER_LAYERS.Point.name, source.getParams());
  }

  function refreshWmsLayer(type) {
    const source = wmsLayersRef.current[type]?.getSource();

    if (!source) {
      return;
    }

    const cqlFilter = buildLayerCqlFilter(type, userInfoRef.current?.userId);
    const params = cqlFilter ? { CQL_FILTER: cqlFilter, _refresh: Date.now() } : { CQL_FILTER: null, _refresh: Date.now() };
    source.updateParams(params);
    debugWmsParams("layer-refresh", GEOSERVER_LAYERS[type].name, source.getParams());

    if (type === "Point") {
      refreshHeatmapLayer();
    }
  }

  function refreshHeatmapLayer() {
    const source = heatmapLayerRef.current?.getSource();

    if (!source) {
      return;
    }

    const cqlFilter = buildUserCqlFilter(userInfoRef.current?.userId);
    const params = cqlFilter ? { CQL_FILTER: cqlFilter, _refresh: Date.now() } : { CQL_FILTER: null, _refresh: Date.now() };
    source.updateParams(params);
    debugWmsParams("heatmap-refresh", GEOSERVER_LAYERS.Point.name, source.getParams());
  }

  function clearSelectionSources() {
    Object.values(vectorSourcesRef.current).forEach((source) => {
      source.getFeatures()
        .filter((feature) => !feature.get("isBackendFallback"))
        .forEach((feature) => source.removeFeature(feature));
    });
  }

  function normalizeFeature(feature, type) {
    const properties = feature.getProperties();
    const id = properties.id ?? properties.Id ?? parseFeatureId(feature.getId());
    const color = properties.color ?? properties.Color ?? DEFAULT_COLOR;

    feature.set("id", id);
    feature.set("type", type);
    feature.set("endpoint", GEOSERVER_LAYERS[type].endpoint);
    feature.set("name", properties.name ?? properties.Name ?? "");
    feature.set("color", color);
    feature.set("modifiedDate", properties.modifiedDate ?? properties.ModifiedDate ?? "");
    if (type === "Poi") {
      feature.set("categoryId", properties.categoryid ?? properties.CategoryId);
      feature.set("workingHours", properties.workinghours ?? properties.WorkingHours ?? "");
      feature.set("iconKey", properties.iconkey ?? properties.IconKey ?? "default");
      feature.setStyle(createPoiIconStyle(feature.get("iconKey")));
    }
    feature.setStyle(createFeatureStyle(color));

    return feature;
  }

  function parseFeatureId(featureId) {
    if (!featureId) {
      return null;
    }

    const idPart = String(featureId).split(".").pop();
    const parsedId = Number(idPart);

    return Number.isNaN(parsedId) ? null : parsedId;
  }

  async function getSelectableFeaturesAtEvent(event) {
    const currentUserId = userInfoRef.current?.userId;

    if (!currentUserId) {
      return [];
    }

    const map = mapInstanceRef.current;

    if (!map) {
      return [];
    }

    const resolution = map.getView().getResolution() || 1;
    const tolerance = resolution * 8;
    const [x, y] = event.coordinate;
    const visibleTypes = Object.entries(layerVisibilityRef.current ?? layerVisibility)
      .filter(([, isVisible]) => isVisible)
      .map(([type]) => type);

    if (visibleTypes.length === 0) {
      return [];
    }

    const vectorFeatures = getVectorFeaturesAtPixel(event.pixel, visibleTypes);

    if (vectorFeatures.length > 0) {
      return vectorFeatures;
    }

    try {
      const results = await getFeaturesFromLayers({
        types: visibleTypes,
        userId: currentUserId,
        bbox: [x - tolerance, y - tolerance, x + tolerance, y + tolerance],
        maxFeatures: 10
      });

      clearSelectionSources();

      return results
        .map(({ feature, type }) => normalizeFeature(feature, type))
        .filter((feature) => feature.get("id"))
        .map((feature) => {
          getSourceForType(feature.get("type"))?.addFeature(feature);
          return feature;
        });
    } catch {
      return [];
    }
  }

  function getVectorFeaturesAtPixel(pixel, visibleTypes) {
    const map = mapInstanceRef.current;

    if (!map) {
      return [];
    }

    const features = [];

    map.forEachFeatureAtPixel(
      pixel,
      (feature) => {
        const type = feature.get("type");

        if (feature.get("id") && visibleTypes.includes(type) && !features.includes(feature)) {
          features.push(feature);
        }
      },
      {
        hitTolerance: 8,
        layerFilter: (layer) => Object.values(vectorLayersRef.current).includes(layer)
      }
    );

    return features;
  }

  function getSourceForEndpoint(endpoint) {
    const type = Object.keys(drawSettings).find((key) => drawSettings[key].endpoint === endpoint);

    return type ? getSourceForType(type) : null;
  }

  function getTypeForEndpoint(endpoint) {
    return Object.keys(drawSettings).find((key) => drawSettings[key].endpoint === endpoint) ?? "";
  }

  function getSourceForFeature(feature) {
    return getSourceForType(feature.get("type"));
  }

  function handleLayerVisibilityChange(event) {
    const { name, checked } = event.target;

    setLayerVisibility((current) => ({
      ...current,
      [name]: checked
    }));
  }

  function handleQueryFilterChange(event) {
    const { name, value } = event.target;

    setQueryFilters((current) => ({
      ...current,
      [name]: value
    }));
  }

  function findFeatureByRecord(record) {
    const source = getSourceForType(record.type ?? record.Type);
    const recordId = record.id ?? record.Id;
    const endpoint = record.endpoint ?? record.Endpoint;

    if (!source) {
      return null;
    }

    return source.getFeatures().find((feature) =>
      feature.get("id") === recordId &&
      feature.get("endpoint") === endpoint
    ) ?? null;
  }

  function focusQueryResult(record) {
    const feature = findFeatureByRecord(record) ?? createFeatureFromRecord(record);

    if (!feature) {
      return;
    }

    if (!findFeatureByRecord(record)) {
      feature.set("isBackendFallback", true);
      getSourceForType(feature.get("type"))?.addFeature(feature);
    }

    setSelectionFeatures([]);
    openDetailPopup(feature);

    const geometry = feature.getGeometry();
    const map = mapInstanceRef.current;

    if (geometry && map) {
      map.getView().fit(geometry.getExtent(), {
        duration: 350,
        maxZoom: 15,
        padding: [90, 90, 90, 90]
      });
    }
  }

  function createFeatureFromRecord(record) {
    const wkt = record.wkt ?? record.Wkt;

    if (!wkt) {
      return null;
    }

    const feature = wktFormatRef.current.readFeature(wkt, {
      dataProjection: "EPSG:4326",
      featureProjection: "EPSG:3857"
    });
    const color = record.color ?? record.Color ?? DEFAULT_COLOR;

    feature.set("id", record.id ?? record.Id);
    feature.set("type", record.type ?? record.Type);
    feature.set("endpoint", record.endpoint ?? record.Endpoint);
    feature.set("name", record.name ?? record.Name ?? "");
    feature.set("color", color);
    feature.set("modifiedDate", record.modifiedDate ?? record.ModifiedDate ?? "");
    feature.setStyle(createFeatureStyle(color));

    return feature;
  }

  function syncFallbackFeatures(records) {
    Object.values(vectorSourcesRef.current).forEach((source) => source.clear());
    heatmapSourceRef.current.clear();

    records.forEach((record) => {
      const feature = createFeatureFromRecord(record);

      if (!feature) {
        return;
      }

      feature.set("isBackendFallback", true);
      getSourceForType(feature.get("type"))?.addFeature(feature);

      if (feature.get("type") === "Point") {
        const heatmapFeature = createFeatureFromRecord(record);

        if (heatmapFeature) {
          heatmapSourceRef.current.addFeature(heatmapFeature);
        }
      }
    });
  }

  function formatModifiedDate(value) {
    if (!value) {
      return "-";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return "-";
    }

    return date.toLocaleString("tr-TR");
  }

  function startDrawing(tool) {
    setSelectionFeatures([]);
    closeDetailPopup();
    setActiveTool(tool);
  }

  function updateCriterion(index, field, value) { setCriteria(items => items.map((item, i) => i === index ? { ...item, [field]: value } : item)); }
  function clearLocationAnalysis(close = false) {
    analysisSourceRef.current.clear(); locationHeatmapLayerRef.current?.setVisible(false); locationHeatmapSourceRef.current.clear(); locationHeatmapVectorLayerRef.current?.setVisible(false); setTargetGeometry(null);
    setCriteria([{ categoryId: "", weight: "" }, { categoryId: "", weight: "" }]); setLocationResult(null); setLocationError(""); setSelectedProvince("");
    if (drawInteractionRef.current) mapInstanceRef.current?.removeInteraction(drawInteractionRef.current); drawInteractionRef.current = null; setActiveTool(null);
    if (close) setLocationPanelOpen(false);
  }
  async function selectProvince(id) {
    setSelectedProvince(id); if (!id) { setTargetGeometry(null); analysisSourceRef.current.clear(); return; }
    try {
      const response = await fetch(API_URL + "/provinces/" + id, { headers: { Authorization: "Bearer " + localStorage.getItem(TOKEN_KEY) } });
      if (!response.ok) throw new Error("İl geometrisi alınamadı."); const data = await response.json();
      const geometry = typeof data.geometry === "string" ? JSON.parse(data.geometry) : data.geometry;
      const feature = geoJsonFormatRef.current.readFeature({ type:"Feature", geometry, properties:{} }, { dataProjection:"EPSG:4326", featureProjection:"EPSG:3857" });
      analysisSourceRef.current.clear(); feature.setStyle(createAnalysisStyle()); analysisSourceRef.current.addFeature(feature); setTargetGeometry(geometry); setLocationError("");
      mapInstanceRef.current?.getView().fit(feature.getGeometry().getExtent(), { padding:[70,420,70,70], duration:500 });
    } catch (error) { setLocationError(error.message); }
  }
  async function runLocationAnalysis() {
    const total = criteria.reduce((sum, x) => sum + Number(x.weight || 0), 0);
    const ids = criteria.map(x => Number(x.categoryId));
    if (!targetGeometry) return setLocationError("Hedef bölge seçilmedi.");
    if (criteria.length < 2 || criteria.length > 5) return setLocationError("En az 2, en fazla 5 kriter seçilmelidir.");
    if (ids.some(x => !x) || new Set(ids).size !== ids.length) return setLocationError("Her kategori seçilmeli ve aynı kategori tekrarlanmamalıdır.");
    if (total !== 100) return setLocationError("Kriter toplamı 100 olmalıdır.");
    setLocationLoading(true); setLocationError("");
    try {
      const response = await fetch(API_URL + "/location-analysis", { method: "POST", headers: { "Content-Type": "application/json", Authorization: "Bearer " + localStorage.getItem(TOKEN_KEY) }, body: JSON.stringify({ targetGeometry, criteria: criteria.map(x => ({ categoryId: Number(x.categoryId), weight: Number(x.weight) })) }) });
      const data = await response.json(); if (!response.ok) throw new Error(data.message || "Analiz sırasında hata oluştu."); setLocationResult(data);
      locationHeatmapSourceRef.current.clear();
      (data.points || []).forEach(point => {
        const feature = new Feature({ geometry: new PointGeometry(fromLonLat([point.longitude, point.latitude])) });
        feature.set("heatWeight", Math.max(0.15, Number(point.heatWeight) || 0)); locationHeatmapSourceRef.current.addFeature(feature);
      });
      locationHeatmapVectorLayerRef.current?.setVisible((data.points || []).length > 0);
      const source = locationHeatmapLayerRef.current.getSource();
      const safeAnalysisId = String(data.analysisId).replace(/[^a-fA-F0-9-]/g, ""); const safeUserId = Number(userInfo?.userId);
      source.updateParams({ CQL_FILTER: "AnalysisId='" + safeAnalysisId + "' AND UserId=" + safeUserId + " AND IsActive=true", _refresh: Date.now() });
      locationHeatmapLayerRef.current.setVisible(true); const extent = analysisSourceRef.current.getExtent(); mapInstanceRef.current?.getView().fit(extent, { padding: [80, 420, 80, 80], duration: 500, maxZoom: 16 });
    } catch (error) { setLocationError(error.message); } finally { setLocationLoading(false); }
  }

  function toggleHeatmapAnalysis() {
    setSelectionFeatures([]);
    closeDetailPopup();
    setActiveTool(null);
    setHeatmapMessage("");
    setIsHeatmapActive((current) => {
      const nextValue = !current;

      heatmapLayerRef.current?.setVisible(nextValue);
      heatmapVectorLayerRef.current?.setVisible(nextValue);

      if (nextValue) {
        refreshHeatmapLayer();
        checkHeatmapPointAvailability();
      }

      return nextValue;
    });
  }

  function checkHeatmapPointAvailability() {
    const currentUserId = userInfoRef.current?.userId;

    if (!currentUserId) {
      setHeatmapMessage("Isı haritası için kullanıcı bilgisi alınamadı.");
      return;
    }

    const pointCount = queryResults.filter((record) => (record.type ?? record.Type) === "Point").length;

    setHeatmapMessage(pointCount === 0 ? "Isı haritası için gösterilecek nokta bulunamadı." : "");
  }

  function clearAnalysis() {
    analysisSourceRef.current.clear();
    setAnalysisMessage("");
  }

  function clearEditInteractions() {
    const map = mapInstanceRef.current;

    if (map) {
      editInteractionsRef.current.forEach((interaction) => {
        map.removeInteraction(interaction);
      });
    }

    editInteractionsRef.current = [];
  }

  function startDetailEditing() {
    if (!selectedFeature) {
      return;
    }

    originalDetailGeometryRef.current = selectedFeature.getGeometry()?.clone() ?? null;
    setIsDetailEditing(true);
    setDetailError("");
  }

  function revertUnsavedDetailGeometry() {
    if (selectedFeature && isDetailEditing && originalDetailGeometryRef.current) {
      selectedFeature.setGeometry(originalDetailGeometryRef.current.clone());
    }

    originalDetailGeometryRef.current = null;
  }

  function openDetailPopup(feature) {
    const color = feature.get("color") || DEFAULT_COLOR;

    setSelectedFeature(feature);
    setDetailForm({
      name: feature.get("name") || "",
      color,
      categoryId: feature.get("categoryId") || "",
      workingHours: feature.get("workingHours") || ""
    });
    setIsDetailEditing(false);
    setIsDetailSaving(false);
    setDetailError("");
    originalDetailGeometryRef.current = null;
    if (feature.get("type") === "Poi" && !feature.get("categoryName")) loadPoiDetail(feature);
  }

  async function loadPoiDetail(feature) {
    const token = localStorage.getItem(TOKEN_KEY);
    try {
      const response = await fetch(API_URL + "/pois/" + feature.get("id"), { headers: { Authorization: "Bearer " + token } });
      if (!response.ok) return;
      const item = await response.json();
      Object.entries({ name:item.name, categoryId:item.categoryId, categoryName:item.categoryName, parentCategoryName:item.parentCategoryName, workingHours:item.workingHours, iconKey:item.iconKey, username:item.username, createdDate:item.createdDate }).forEach(([key,value]) => feature.set(key,value));
      feature.setStyle(createPoiIconStyle(item.iconKey));
      if (selectedFeature === feature) {
        setDetailForm(current => ({ ...current, name:item.name, categoryId:item.categoryId, workingHours:item.workingHours }));
        setSelectedFeature(null); window.setTimeout(() => setSelectedFeature(feature), 0);
      }
    } catch { setDetailError("POI detayı alınamadı."); }
  }

  function closeDetailPopup() {
    revertUnsavedDetailGeometry();
    clearEditInteractions();
    setSelectedFeature(null);
    setIsDetailEditing(false);
    setIsDetailSaving(false);
    setDetailError("");
  }

  function cancelPendingFeature() {
    if (pendingFeature) {
      getSourceForEndpoint(pendingFeature.endpoint)?.removeFeature(pendingFeature.feature);
    }

    setPendingFeature(null);
    setPopupError("");
    setIsSaving(false);
  }

  async function handleAttributeSubmit(event) {
    event.preventDefault();

    if (!pendingFeature || isSaving) {
      return;
    }

    if (!attributeForm.name.trim()) {
      setPopupError("Isim alanini doldurun.");
      return;
    }
    if (pendingFeature.endpoint === "Pois" && !attributeForm.categoryId) {
      setPopupError("Kategori seçin.");
      return;
    }

    setIsSaving(true);
    setPopupError("");

    const saveResult = await saveFeature(
      pendingFeature.feature,
      pendingFeature.endpoint,
      attributeForm.name.trim(),
      attributeForm.color
    );

    if (!saveResult.ok) {
      setIsSaving(false);
      setPopupError(saveResult.message || "Kayit sirasinda bir sorun olustu.");
      return;
    }

    if (pendingFeature.endpoint === "TblPolygon" && typeof saveResult.data?.inventoryCount === "number") {
      setInventoryMessage("Polygon \u00e7inde " + saveResult.data.inventoryCount + " adet nokta var");
    }

    const isPoi = pendingFeature.endpoint === "Pois";
    pendingFeature.feature.set("name", attributeForm.name.trim());
    pendingFeature.feature.set("color", attributeForm.color);
    pendingFeature.feature.set("id", saveResult.data?.id ?? saveResult.data?.Id);
    if (isPoi) {
      const category = poiCategories.find(item => item.id === Number(attributeForm.categoryId));
      pendingFeature.feature.set("categoryId", Number(attributeForm.categoryId));
      pendingFeature.feature.set("categoryName", category?.name || "");
      pendingFeature.feature.set("parentCategoryName", category?.parentName || "");
      pendingFeature.feature.set("workingHours", attributeForm.workingHours || "");
      pendingFeature.feature.set("iconKey", category?.iconKey || "default");
      pendingFeature.feature.set("username", userInfo?.username || "");
      pendingFeature.feature.set("createdDate", new Date().toISOString());
      pendingFeature.feature.set("isBackendFallback", true);
    }
    pendingFeature.feature.set(
      "type",
      getTypeForEndpoint(pendingFeature.endpoint)
    );
    pendingFeature.feature.set("endpoint", pendingFeature.endpoint);
    pendingFeature.feature.setStyle(isPoi ? createPoiIconStyle(pendingFeature.feature.get("iconKey")) : createFeatureStyle(attributeForm.color));
    if (!isPoi) getSourceForEndpoint(pendingFeature.endpoint)?.removeFeature(pendingFeature.feature);
    refreshWmsLayer(getTypeForEndpoint(pendingFeature.endpoint));
    if (isPoi) loadPoiDetail(pendingFeature.feature);
    setPendingFeature(null);
    setIsSaving(false);
    setQueryReloadKey((current) => current + 1);
  }

  async function saveFeature(feature, endpoint, name, color) {
    const geometry = feature.getGeometry().clone().transform("EPSG:3857", "EPSG:4326");
    const wkt = wktFormatRef.current.writeGeometry(geometry);
    const token = localStorage.getItem(TOKEN_KEY);

    if (!token) {
      getSourceForEndpoint(endpoint)?.removeFeature(feature);
      onLogout();
      return { ok: false };
    }

    try {
      const response = await fetch(API_URL + "/" + endpoint, {
        method: "POST",
        headers: {
          "Authorization": "Bearer " + token,
          "Content-Type": "application/json"
        },
        body: JSON.stringify(endpoint === "Pois" ? { wkt, name, categoryId: Number(attributeForm.categoryId), workingHours: attributeForm.workingHours, isActive: true } : { wkt, name, color })
      });

      if (!response.ok) {
        getSourceForEndpoint(endpoint)?.removeFeature(feature);
        return { ok: false, message: await readErrorMessage(response) };
      }

      const data = await response.json();
      return { ok: true, data };
    } catch {
      getSourceForEndpoint(endpoint)?.removeFeature(feature);
      return { ok: false, message: "Sunucuya baglanilamadi." };
    }
  }

  async function readErrorMessage(response) {
    try {
      const text = await response.text();

      if (text) {
        return text;
      }
    } catch {
      return "Kayit sirasinda bir sorun olustu.";
    }

    if (response.status === 403) {
      return "Bu islem icin yetkiniz veya cografi yetki alaniniz uygun degil.";
    }

    if (response.status === 400) {
      return "Cizilen geometri gecersiz.";
    }

    return "Kayit sirasinda bir sorun olustu.";
  }

  async function analyzeInventory(feature) {
    const geometry = feature.getGeometry().clone().transform("EPSG:3857", "EPSG:4326");
    const wkt = wktFormatRef.current.writeGeometry(geometry);
    const token = localStorage.getItem(TOKEN_KEY);

    if (!token) {
      onLogoutRef.current();
      return;
    }

    setAnalysisMessage("Analiz yapiliyor...");

    try {
      const response = await fetch(API_URL + "/InventoryAnalysis", {
        method: "POST",
        headers: {
          "Authorization": "Bearer " + token,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ wkt })
      });

      if (response.status === 401) {
        onLogoutRef.current();
        return;
      }

      if (!response.ok) {
        setAnalysisMessage("Analiz sirasinda bir sorun olustu.");
        return;
      }

      const data = await response.json();
      const pointCount = data.pointCount ?? data.PointCount ?? 0;
      const lineCount = data.lineCount ?? data.LineCount ?? 0;
      const polygonCount = data.polygonCount ?? data.PolygonCount ?? 0;
      const totalCount = data.totalCount ?? data.TotalCount ?? pointCount + lineCount + polygonCount;

      setAnalysisMessage(
        "Analiz sonucu: " +
        pointCount +
        " nokta, " +
        lineCount +
        " cizgi, " +
        polygonCount +
        " polygon. Toplam " +
        totalCount +
        " envanter."
      );
    } catch {
      setAnalysisMessage("Sunucuya baglanilamadi.");
    }
  }

  async function handleDetailSave() {
    if (!selectedFeature || isDetailSaving) {
      return;
    }

    if (!detailForm.name.trim()) {
      setDetailError("Isim alanini doldurun.");
      return;
    }

    const endpoint = selectedFeature.get("endpoint");
    const id = selectedFeature.get("id");
    const token = localStorage.getItem(TOKEN_KEY);

    if (!endpoint || !id || !token) {
      onLogoutRef.current();
      return;
    }

    const geometry = selectedFeature.getGeometry().clone().transform("EPSG:3857", "EPSG:4326");
    const wkt = wktFormatRef.current.writeGeometry(geometry);

    setIsDetailSaving(true);
    setDetailError("");

    try {
      const response = await fetch(API_URL + "/" + endpoint + "/" + id, {
        method: "PUT",
        headers: {
          "Authorization": "Bearer " + token,
          "Content-Type": "application/json"
        },
        body: JSON.stringify(endpoint === "Pois"
          ? { wkt, name: detailForm.name.trim(), categoryId: Number(detailForm.categoryId), workingHours: detailForm.workingHours, isActive: true }
          : { wkt, name: detailForm.name.trim(), color: detailForm.color })
      });

      if (response.status === 401) {
        onLogoutRef.current();
        return;
      }

      if (!response.ok) {
        setDetailError("Guncelleme sirasinda bir sorun olustu.");
        return;
      }

      selectedFeature.set("name", detailForm.name.trim());
      selectedFeature.set("color", detailForm.color);
      selectedFeature.setStyle(createFeatureStyle(detailForm.color));
      originalDetailGeometryRef.current = null;
      setIsDetailEditing(false);
      refreshWmsLayer(selectedFeature.get("type"));
      setQueryReloadKey((current) => current + 1);
    } catch {
      setDetailError("Sunucuya baglanilamadi.");
    } finally {
      setIsDetailSaving(false);
    }
  }

  async function handleDetailDelete() {
    if (!selectedFeature || isDetailSaving) {
      return;
    }

    setDeleteConfirmOpen(true);
  }

  async function confirmDetailDelete() {
    if (!selectedFeature || isDetailSaving) {
      return;
    }

    const endpoint = selectedFeature.get("endpoint");
    const id = selectedFeature.get("id");
    const token = localStorage.getItem(TOKEN_KEY);

    if (!endpoint || !id || !token) {
      onLogoutRef.current();
      return;
    }

    setIsDetailSaving(true);
    setDeleteConfirmOpen(false);
    setDetailError("");

    try {
      const response = await fetch(API_URL + "/" + endpoint + "/" + id, {
        method: "DELETE",
        headers: {
          "Authorization": "Bearer " + token
        }
      });

      if (response.status === 401) {
        onLogoutRef.current();
        return;
      }

      if (!response.ok) {
        setDetailError("Silme sirasinda bir sorun olustu.");
        return;
      }

      getSourceForFeature(selectedFeature)?.removeFeature(selectedFeature);
      refreshWmsLayer(selectedFeature.get("type"));
      closeDetailPopup();
      setQueryReloadKey((current) => current + 1);
    } catch {
      setDetailError("Sunucuya baglanilamadi.");
    } finally {
      setIsDetailSaving(false);
    }
  }

  function cancelDetailDelete() {
    if (!isDetailSaving) {
      setDeleteConfirmOpen(false);
    }
  }

  return (
    <main className="map-page">
      <Navbar
        activeTool={activeTool}
        isHeatmapActive={isHeatmapActive}
        remainingSeconds={remainingSeconds}
        userInfo={userInfo}
        onAdminOpen={onAdminOpen}
        onLogout={onLogout}
        onStartDrawing={startDrawing}
        onToggleHeatmap={toggleHeatmapAnalysis}
        onLocationAnalysis={() => setLocationPanelOpen(true)}
      />
      {false && (
      <header className="topbar">
        <div>
          <span className="topbar-kicker">OpenLayers</span>
          <h1>Dünya Haritası</h1>
        </div>
        <div className="topbar-actions">
          <div className="session-timer">{formatTime(remainingSeconds)}</div>
        </div>
      </header>
      )}

      <section className="map-shell">
        {locationPanelOpen && <aside className="location-analysis-panel">
          <div className="location-panel-header"><h2>Konum Analizi</h2><button type="button" onClick={() => clearLocationAnalysis(true)}>×</button></div>
          <section><h3>1. Hedef Bölge</h3><div className="target-method"><label><input type="radio" checked={targetMethod === "draw"} onChange={() => setTargetMethod("draw")}/> Polygon Çiz</label><label><input type="radio" checked={targetMethod === "province"} onChange={() => setTargetMethod("province")}/> İl Seç</label></div>{targetMethod === "draw" ? <button type="button" className="secondary-button" onClick={() => startDrawing("LocationAnalysis")}>{targetGeometry ? "Polygonu Yeniden Çiz" : "Haritada Polygon Çiz"}</button> : <select value={selectedProvince} onChange={e => selectProvince(e.target.value)}><option value="">İl seçin</option>{provinces.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select>}<small>{targetGeometry ? "Hedef alan hazır" : targetMethod === "province" && provinces.length === 0 ? "İl sınırı verisi henüz yüklenmemiş" : "Bir hedef bölge seçmeniz gerekir"}</small></section>
          <section><div className="criteria-title"><h3>2. Kriterler</h3><button type="button" disabled={criteria.length >= 5} onClick={() => setCriteria(x => [...x, { categoryId:"", weight:"" }])}>Kriter Ekle</button></div>
            {criteria.map((criterion, index) => <div className="criterion-row" key={index}><select value={criterion.categoryId} onChange={e => updateCriterion(index, "categoryId", e.target.value)}><option value="">POI kategorisi</option>{poiCategories.map(c => <option key={c.id} value={c.id} disabled={criteria.some((x,i) => i !== index && String(x.categoryId) === String(c.id))}>{c.displayName}</option>)}</select><input type="number" min="1" max="100" value={criterion.weight} onChange={e => updateCriterion(index, "weight", e.target.value)} placeholder="Puan"/><button type="button" disabled={criteria.length <= 2} onClick={() => setCriteria(x => x.filter((_,i) => i !== index))}>Sil</button></div>)}
          </section>
          <div className={criteria.reduce((s,x) => s + Number(x.weight || 0),0) === 100 ? "location-total valid" : "location-total"}>Toplam Puan: {criteria.reduce((s,x) => s + Number(x.weight || 0),0)} / 100</div>
          {locationError && <div className="location-error">{locationError}</div>}
          <button type="button" className="primary-button" disabled={locationLoading || !targetGeometry || criteria.reduce((s,x) => s + Number(x.weight || 0),0) !== 100} onClick={runLocationAnalysis}>{locationLoading ? "Analiz yapılıyor..." : "Analizi Başlat"}</button>
          <button type="button" className="secondary-button" onClick={() => clearLocationAnalysis(false)}>Temizle</button>
          {locationResult && <div className="location-summary"><strong>{locationResult.message}</strong><span>Toplam {locationResult.matchedPoiCount} POI</span>{locationResult.criteriaSummary?.map(x => <span key={x.categoryId}>{x.categoryName} — {x.matchedPoiCount} POI — {x.weight} puan</span>)}</div>}
        </aside>}
        {(userInfo?.permissions ?? []).includes("Poi.Search") && <div className="poi-search-box">
          <input value={poiSearch} onChange={event => setPoiSearch(event.target.value)} placeholder="POI veya kategori ara" aria-label="POI ara" />
          {poiSearchError && <div className="poi-search-error">{poiSearchError}</div>}
          {poiSearchResults.length > 0 && <div className="poi-search-results">{poiSearchResults.map(item => <button type="button" key={item.id} onClick={() => selectPoiSearchResult(item)}><strong>{item.name}</strong><span>{item.parentCategoryName ? item.parentCategoryName + " > " : ""}{item.categoryName}</span><small>{item.workingHours || "Mesai saati belirtilmemiş"}</small></button>)}</div>}
        </div>}
        {false && (
        <div className="draw-toolbar" aria-label="Cizim araclari">
          <button
            type="button"
            className={activeTool === "Point" ? "active-tool" : ""}
            onClick={() => startDrawing("Point")}
          >
            Point
          </button>
          <button
            type="button"
            className={activeTool === "Line" ? "active-tool" : ""}
            onClick={() => startDrawing("Line")}
          >
            Line
          </button>
          <button
            type="button"
            className={activeTool === "Polygon" ? "active-tool" : ""}
            onClick={() => startDrawing("Polygon")}
          >
            Polygon
          </button>
          <button
            type="button"
            className={activeTool === "InventoryAnalysis" ? "active-tool" : ""}
            onClick={() => startDrawing("InventoryAnalysis")}
          >
            Envanter Analizi
          </button>
          <button type="button" className="close-tool" onClick={onLogout}>Close</button>
        </div>
        )}
        <div className="layer-control" aria-label="Layer kontrol">
          <label>
            <input type="checkbox" name="Poi" checked={layerVisibility.Poi} onChange={handleLayerVisibilityChange} />
            POI
          </label>
          <label>
            <input
              type="checkbox"
              name="Point"
              checked={layerVisibility.Point}
              onChange={handleLayerVisibilityChange}
            />
            Point
          </label>
          <label>
            <input
              type="checkbox"
              name="Line"
              checked={layerVisibility.Line}
              onChange={handleLayerVisibilityChange}
            />
            Line
          </label>
          <label>
            <input
              type="checkbox"
              name="Polygon"
              checked={layerVisibility.Polygon}
              onChange={handleLayerVisibilityChange}
            />
            Polygon
          </label>
        </div>
        <div className="query-panel" aria-label="Sorgulama paneli">
          <div className="query-panel-header">
            <h2>Sorgulama</h2>
            <span>{queryResults.length}</span>
          </div>

          <div className="query-filters">
            <label>
              Tip
              <select name="type" value={queryFilters.type} onChange={handleQueryFilterChange}>
                <option value="">Hepsi</option>
                <option value="Point">Point</option>
                <option value="Line">Line</option>
                <option value="Polygon">Polygon</option>
              </select>
            </label>

            <label>
              Ad
              <input
                type="text"
                name="name"
                value={queryFilters.name}
                onChange={handleQueryFilterChange}
                placeholder="Ara"
              />
            </label>

            <label>
              Sıralama
              <select name="sortBy" value={queryFilters.sortBy} onChange={handleQueryFilterChange}>
                <option value="name">Name</option>
                <option value="modifiedDate">Modified Date</option>
                <option value="id">Id</option>
              </select>
            </label>

            <label>
              Yön
              <select name="sortDirection" value={queryFilters.sortDirection} onChange={handleQueryFilterChange}>
                <option value="asc">Asc</option>
                <option value="desc">Desc</option>
              </select>
            </label>
          </div>

          {queryError && <div className="query-error">{queryError}</div>}

          <div className="query-results">
            {queryResults.map((record) => (
              <button
                type="button"
                key={(record.endpoint ?? record.Endpoint) + "-" + (record.id ?? record.Id)}
                onClick={() => focusQueryResult(record)}
              >
                <span className="query-type">{record.type ?? record.Type}</span>
                <span className="query-name">{(record.name ?? record.Name) || "Isimsiz obje"}</span>
                <span className="query-color" style={{ background: record.color ?? record.Color }} />
                <span className="query-date">{formatModifiedDate(record.modifiedDate ?? record.ModifiedDate)}</span>
              </button>
            ))}
            {!queryError && queryResults.length === 0 && (
              <div className="query-empty">Kayit yok</div>
            )}
          </div>
        </div>
        {inventoryMessage && (
          <div className="inventory-message">{inventoryMessage}</div>
        )}
        {analysisMessage && (
          <div className="analysis-message">
            <span>{analysisMessage}</span>
            <button type="button" onClick={clearAnalysis}>Analizi Temizle</button>
          </div>
        )}
        {isHeatmapActive && (
          <div className="heatmap-legend" aria-label="Isı haritası lejantı">
            <h2>Isı Haritası</h2>
            <div className="heatmap-gradient" aria-hidden="true" />
            <div className="heatmap-scale">
              <span>0.0 düşük yoğunluk</span>
              <span>0.5 orta yoğunluk</span>
              <span>1.0 yüksek yoğunluk</span>
            </div>
            <p>Renkler point yoğunluğunu temsil eder.</p>
            {heatmapMessage && <p className="heatmap-message">{heatmapMessage}</p>}
          </div>
        )}
        <div ref={mapElementRef} className="map-view" />

        {pendingFeature && (
          <div className="attribute-modal-backdrop" role="presentation">
            <form className="attribute-modal" onSubmit={handleAttributeSubmit}>
              <h2>Detaylar</h2>
              <label>
                Isim
                <input
                  type="text"
                  name="name"
                  value={attributeForm.name}
                  onChange={handleAttributeChange}
                  autoFocus
                />
              </label>
              {pendingFeature.endpoint === "Pois" ? <>
                <label>Kategori<select name="categoryId" value={attributeForm.categoryId || ""} onChange={handleAttributeChange}><option value="">Kategori seçin</option>{poiCategories.map(c => <option key={c.id} value={c.id}>{c.displayName}</option>)}</select></label>
                <label>Mesai saatleri<input name="workingHours" value={attributeForm.workingHours || ""} onChange={handleAttributeChange} placeholder="09:00 - 18:00" /></label>
              </> : <label>Renk<ColorPickerField value={attributeForm.color} onChange={handleAttributeColorChange} /></label>}
              <div className="attribute-message">{popupError}</div>
              <div className="attribute-actions">
                <button type="button" className="secondary-button" onClick={cancelPendingFeature} disabled={isSaving}>
                  İptal
                </button>
                <button type="submit" className="primary-button" disabled={isSaving}>
                  {isSaving ? "Kaydediliyor" : "Kaydet"}
                </button>
              </div>
            </form>
          </div>
        )}

        {selectedFeature && !pendingFeature && (
          <div className="detail-popup">
            <div className="detail-popup-header">
              <h2>Obje Detayı</h2>
              <span>{selectedFeature.get("type")}</span>
            </div>

            <label>
              Ad
              <input
                type="text"
                name="name"
                value={detailForm.name}
                onChange={handleDetailChange}
                disabled={!isDetailEditing || isDetailSaving}
              />
            </label>

            <label>
              Renk
              <ColorPickerField
                value={detailForm.color}
                onChange={handleDetailColorChange}
                disabled={!isDetailEditing || isDetailSaving}
              />
            </label>

            <div className="detail-field">
              <span>Tip</span>
              <strong>{selectedFeature.get("type")}</strong>
            </div>
            {selectedFeature.get("type") === "Poi" && <>
              <div className="detail-field"><span>Kategori</span><strong>{selectedFeature.get("categoryName") || "-"}</strong></div>
              <div className="detail-field"><span>Üst kategori</span><strong>{selectedFeature.get("parentCategoryName") || "-"}</strong></div>
              <div className="detail-field"><span>Mesai saatleri</span><strong>{selectedFeature.get("workingHours") || "-"}</strong></div>
              <div className="detail-field"><span>Ekleyen</span><strong>{selectedFeature.get("username") || "-"}</strong></div>
              <div className="detail-field"><span>Oluşturulma</span><strong>{selectedFeature.get("createdDate") ? new Date(selectedFeature.get("createdDate")).toLocaleString("tr-TR") : "-"}</strong></div>
            </>}

            <div className="attribute-message">{detailError}</div>

            <div className="detail-actions">
              {(selectedFeature.get("type") !== "Poi" || (userInfo?.permissions ?? []).some(p => p === "Poi.Update" || p === "Poi.Manage")) && <button type="button" className="secondary-button" onClick={startDetailEditing} disabled={isDetailEditing || isDetailSaving}>
                Düzenle
              </button>}
              <button type="button" className="primary-button" onClick={handleDetailSave} disabled={!isDetailEditing || isDetailSaving}>
                {isDetailSaving && isDetailEditing ? "Kaydediliyor" : "Kaydet"}
              </button>
              {(selectedFeature.get("type") !== "Poi" || (userInfo?.permissions ?? []).some(p => p === "Poi.Delete" || p === "Poi.Manage")) && <button type="button" className="danger-button" onClick={handleDetailDelete} disabled={isDetailSaving}>
                Sil
              </button>}
              <button type="button" className="secondary-button" onClick={closeDetailPopup} disabled={isDetailSaving}>
                Kapat
              </button>
            </div>
          </div>
        )}

        {deleteConfirmOpen && selectedFeature && !pendingFeature && (
          <div className="attribute-modal-backdrop confirm-modal-backdrop" role="presentation">
            <div className="confirm-modal" role="dialog" aria-modal="true" aria-labelledby="delete-confirm-title">
              <h2 id="delete-confirm-title">Objeyi sil</h2>
              <p>
                {selectedFeature.get("name") || "Isimsiz obje"} kaydı silinecek. Bu işlem geri alınamaz.
              </p>
              <div className="confirm-actions">
                <button type="button" className="secondary-button" onClick={cancelDetailDelete} disabled={isDetailSaving}>
                  Vazgeç
                </button>
                <button type="button" className="danger-button" onClick={confirmDetailDelete} disabled={isDetailSaving}>
                  {isDetailSaving ? "Siliniyor" : "Sil"}
                </button>
              </div>
            </div>
          </div>
        )}

        {selectionFeatures.length > 1 && !pendingFeature && (
          <div className="selection-modal">
            <div className="selection-modal-header">
              <h2>Obje Seç</h2>
              <button type="button" onClick={() => setSelectionFeatures([])}>Kapat</button>
            </div>
            <div className="selection-list">
              {selectionFeatures.map((feature) => (
                <button
                  type="button"
                  key={feature.get("endpoint") + "-" + feature.get("id")}
                  onClick={() => {
                    setSelectionFeatures([]);
                    openDetailPopup(feature);
                  }}
                >
                  <span>{feature.get("type")}</span>
                  <strong>{feature.get("name") || "Isimsiz obje"}</strong>
                </button>
              ))}
            </div>
          </div>
        )}
      </section>
    </main>
  );
}

export default Map;
