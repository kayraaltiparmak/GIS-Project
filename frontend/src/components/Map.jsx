import { useEffect, useRef, useState } from "react";
import "ol/ol.css";
import OLMap from "ol/Map";
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
import { fromLonLat } from "ol/proj";
import Style from "ol/style/Style";
import Stroke from "ol/style/Stroke";
import Fill from "ol/style/Fill";
import CircleStyle from "ol/style/Circle";
import { unByKey } from "ol/Observable";
import Navbar from "./Navbar";
import {
  GEOSERVER_DEBUG,
  GEOSERVER_HEATMAP_STYLE,
  GEOSERVER_LAYERS,
  GEOSERVER_WMS_URL
} from "../config/geoserver";
import { buildUserCqlFilter, getFeaturesFromLayers } from "../services/geoserverWfs";

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
  Point: { drawType: "Point", endpoint: "TblPoint" },
  Line: { drawType: "LineString", endpoint: "TblLine" },
  Polygon: { drawType: "Polygon", endpoint: "TblPolygon" },
  InventoryAnalysis: { drawType: "Polygon", endpoint: "InventoryAnalysis" }
};

const WMS_Z_INDEX = {
  Polygon: 10,
  Line: 20,
  Point: 30,
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
    Point: true,
    Line: true,
    Polygon: true
  });
  const [attributeForm, setAttributeForm] = useState({ name: "", color: DEFAULT_COLOR });
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

  useEffect(() => {
    onLogoutRef.current = onLogout;
  }, [onLogout]);

  useEffect(() => {
    userInfoRef.current = userInfo;
  }, [userInfo]);

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
      Point: createWmsLayer("Point", userInfo?.userId, layerVisibility.Point)
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

    mapInstanceRef.current = new OLMap({
      target: mapElementRef.current,
      layers: [
        new TileLayer({
          source: new OSM()
        }),
        wmsLayersRef.current.Polygon,
        wmsLayersRef.current.Line,
        wmsLayersRef.current.Point,
        heatmapLayerRef.current,
        heatmapVectorLayerRef.current,
        vectorLayersRef.current.Polygon,
        vectorLayersRef.current.Line,
        vectorLayersRef.current.Point,
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
    Object.entries(layerVisibility).forEach(([type, isVisible]) => {
      wmsLayersRef.current[type]?.setVisible(isVisible);
      vectorLayersRef.current[type]?.setVisible(isVisible);
    });
  }, [layerVisibility]);

  useEffect(() => {
    Object.keys(GEOSERVER_LAYERS).forEach((type) => updateWmsCqlFilter(type));
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
      source: activeTool === "InventoryAnalysis" ? analysisSourceRef.current : getSourceForType(activeTool),
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

      setPendingFeature({ feature: event.feature, endpoint: setting.endpoint });
      setAttributeForm({ name: "", color: DEFAULT_COLOR });
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
    const cqlFilter = buildUserCqlFilter(userId);
    const params = {
      LAYERS: layer.name,
      TILED: true,
      VERSION: "1.1.0",
      FORMAT: "image/png",
      TRANSPARENT: true
    };

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

    const cqlFilter = buildUserCqlFilter(userInfoRef.current?.userId);
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

    const cqlFilter = buildUserCqlFilter(userInfoRef.current?.userId);
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
        cqlFilter: buildUserCqlFilter(currentUserId),
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
      color
    });
    setIsDetailEditing(false);
    setIsDetailSaving(false);
    setDetailError("");
    originalDetailGeometryRef.current = null;
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

    pendingFeature.feature.set("name", attributeForm.name.trim());
    pendingFeature.feature.set("color", attributeForm.color);
    pendingFeature.feature.set("id", saveResult.data?.id ?? saveResult.data?.Id);
    pendingFeature.feature.set(
      "type",
      getTypeForEndpoint(pendingFeature.endpoint)
    );
    pendingFeature.feature.set("endpoint", pendingFeature.endpoint);
    pendingFeature.feature.setStyle(createFeatureStyle(attributeForm.color));
    getSourceForEndpoint(pendingFeature.endpoint)?.removeFeature(pendingFeature.feature);
    refreshWmsLayer(getTypeForEndpoint(pendingFeature.endpoint));
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
        body: JSON.stringify({ wkt, name, color })
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
        body: JSON.stringify({
          wkt,
          name: detailForm.name.trim(),
          color: detailForm.color
        })
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
              <label>
                Renk
                <ColorPickerField value={attributeForm.color} onChange={handleAttributeColorChange} />
              </label>
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

            <div className="attribute-message">{detailError}</div>

            <div className="detail-actions">
              <button type="button" className="secondary-button" onClick={startDetailEditing} disabled={isDetailEditing || isDetailSaving}>
                Düzenle
              </button>
              <button type="button" className="primary-button" onClick={handleDetailSave} disabled={!isDetailEditing || isDetailSaving}>
                {isDetailSaving && isDetailEditing ? "Kaydediliyor" : "Kaydet"}
              </button>
              <button type="button" className="danger-button" onClick={handleDetailDelete} disabled={isDetailSaving}>
                Sil
              </button>
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

