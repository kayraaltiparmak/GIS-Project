import { useEffect, useRef, useState } from "react";
import "ol/ol.css";
import OLMap from "ol/Map";
import View from "ol/View";
import TileLayer from "ol/layer/Tile";
import VectorLayer from "ol/layer/Vector";
import OSM from "ol/source/OSM";
import VectorSource from "ol/source/Vector";
import Draw from "ol/interaction/Draw";
import WKT from "ol/format/WKT";
import { fromLonLat } from "ol/proj";

const TOKEN_KEY = "token";
const TURKEY_CENTER = [35.2433, 38.9637];
const API_URL = "http://localhost:5291/api";

const drawSettings = {
  Point: { drawType: "Point", endpoint: "TblPoint" },
  Line: { drawType: "LineString", endpoint: "TblLine" },
  Polygon: { drawType: "Polygon", endpoint: "TblPolygon" }
};

function formatTime(totalSeconds) {
  const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return minutes + ":" + seconds;
}

function Map({ remainingSeconds, onLogout }) {
  const mapElementRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const drawInteractionRef = useRef(null);
  const vectorSourceRef = useRef(new VectorSource());
  const wktFormatRef = useRef(new WKT());
  const [activeTool, setActiveTool] = useState(null);

  useEffect(() => {
    if (!mapElementRef.current || mapInstanceRef.current) {
      return;
    }

    mapInstanceRef.current = new OLMap({
      target: mapElementRef.current,
      layers: [
        new TileLayer({
          source: new OSM()
        }),
        new VectorLayer({
          source: vectorSourceRef.current
        })
      ],
      view: new View({
        center: fromLonLat(TURKEY_CENTER),
        zoom: 6
      })
    });

    return () => {
      mapInstanceRef.current?.setTarget(undefined);
      mapInstanceRef.current = null;
    };
  }, []);

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
      source: vectorSourceRef.current,
      type: setting.drawType
    });

    draw.on("drawend", (event) => {
      saveFeature(event.feature, setting.endpoint);
      if (activeTool === "Line") {
        map.removeInteraction(draw);
        drawInteractionRef.current = null;
        setActiveTool(null);
      }
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

  async function saveFeature(feature, endpoint) {
    const geometry = feature.getGeometry().clone().transform("EPSG:3857", "EPSG:4326");
    const wkt = wktFormatRef.current.writeGeometry(geometry);
    const token = localStorage.getItem(TOKEN_KEY);

    if (!token) {
      onLogout();
      return;
    }

    try {
      const response = await fetch(API_URL + "/" + endpoint, {
        method: "POST",
        headers: {
          "Authorization": "Bearer " + token,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ wkt })
      });

      if (!response.ok) {
        vectorSourceRef.current.removeFeature(feature);
      }
    } catch {
      vectorSourceRef.current.removeFeature(feature);
    }
  }

  return (
    <main className="map-page">
      <header className="topbar">
        <div>
          <span className="topbar-kicker">OpenLayers</span>
          <h1>Dünya Haritası</h1>
        </div>
        <div className="topbar-actions">
          <div className="session-timer">{formatTime(remainingSeconds)}</div>
        </div>
      </header>

      <section className="map-shell">
        <div className="draw-toolbar" aria-label="Cizim araclari">
          <button
            type="button"
            className={activeTool === "Point" ? "active-tool" : ""}
            onClick={() => setActiveTool("Point")}
          >
            Point
          </button>
          <button
            type="button"
            className={activeTool === "Line" ? "active-tool" : ""}
            onClick={() => setActiveTool("Line")}
          >
            Line
          </button>
          <button
            type="button"
            className={activeTool === "Polygon" ? "active-tool" : ""}
            onClick={() => setActiveTool("Polygon")}
          >
            Polygon
          </button>
          <button type="button" className="close-tool" onClick={onLogout}>Close</button>
        </div>
        <div ref={mapElementRef} className="map-view" />
      </section>
    </main>
  );
}

export default Map;
