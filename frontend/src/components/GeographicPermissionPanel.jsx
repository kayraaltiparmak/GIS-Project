import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "leaflet-draw/dist/leaflet.draw.css";
import "leaflet-draw";

const API_URL = "http://localhost:5291/api/Admin";
const TURKEY_CENTER = [39, 35];
const TURKEY_PERMISSION_WKT = "POLYGON((25 35, 45 35, 45 43, 25 43, 25 35))";

function latLngsToWkt(latLngs) {
  if (!latLngs.length) {
    return "";
  }

  const coordinates = latLngs.map((latLng) => `${latLng.lng} ${latLng.lat}`);

  if (coordinates[0] !== coordinates[coordinates.length - 1]) {
    coordinates.push(coordinates[0]);
  }

  return `POLYGON((${coordinates.join(", ")}))`;
}

function normalizePolygonLatLngs(latLngs) {
  if (!Array.isArray(latLngs) || latLngs.length === 0) {
    return [];
  }

  let normalized = latLngs;

  while (Array.isArray(normalized[0])) {
    normalized = normalized[0];
  }

  return normalized.filter((latLng) =>
    Number.isFinite(latLng?.lat) &&
    Number.isFinite(latLng?.lng)
  );
}

function wktToLatLngs(wkt) {
  const match = wkt?.match(/POLYGON\s*\(\((.*)\)\)/i);

  if (!match) {
    return [];
  }

  return match[1]
    .split(",")
    .map((pair) => pair.trim().split(/\s+/).map(Number))
    .filter(([lng, lat]) => Number.isFinite(lng) && Number.isFinite(lat))
    .map(([lng, lat]) => [lat, lng]);
}

function GeographicPermissionPanel({ token, userInfo }) {
  const mapRef = useRef(null);
  const mapElementRef = useRef(null);
  const drawnItemsRef = useRef(null);
  const isDrawingRef = useRef(false);
  const draftLatLngsRef = useRef([]);
  const activeEditRef = useRef(null);
  const activeLayerRef = useRef(null);
  const selectedIdRef = useRef("");
  const selectElementRef = useRef(null);
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [targetType, setTargetType] = useState(userInfo?.permissions?.includes("User.Manage") ? "user" : "role");
  const [selectedId, setSelectedId] = useState("");
  const [activeRecord, setActiveRecord] = useState(null);
  const [currentWkt, setCurrentWkt] = useState("");
  const [hasPolygon, setHasPolygon] = useState(false);
  const [message, setMessage] = useState("");

  const canManageUsers = userInfo?.permissions?.includes("User.Manage");
  const canManageRoles = userInfo?.permissions?.includes("Role.Manage");
  const selectedEndpoint = targetType === "user" ? "user-geographic-permissions" : "role-geographic-permissions";
  const selectedList = targetType === "user" ? users : roles;

  const selectedLabel = useMemo(() => {
    const record = selectedList.find((item) => String(item.id) === String(selectedId));
    return record?.username ?? record?.name ?? "";
  }, [selectedId, selectedList]);

  useEffect(() => {
    selectedIdRef.current = selectedId;
  }, [selectedId]);

  async function apiRequest(path, options = {}) {
    const response = await fetch(API_URL + path, {
      ...options,
      headers: {
        "Authorization": "Bearer " + token,
        "Content-Type": "application/json",
        ...(options.headers ?? {})
      }
    });

    if (!response.ok) {
      let message = response.status === 403 ? "Bu işlem için yetkiniz yok." : "İşlem tamamlanamadı.";

      try {
        const text = await response.text();

        if (text) {
          message = text;
        }
      } catch {
        // Keep the status-based message.
      }

      throw new Error(message);
    }

    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  useEffect(() => {
    async function loadOptions() {
      try {
        const requests = [];

        if (canManageUsers) {
          requests.push(apiRequest("/user-options").then(setUsers));
        }

        if (canManageRoles) {
          requests.push(apiRequest("/role-options").then(setRoles));
        }

        await Promise.all(requests);
      } catch (error) {
        setMessage(error.message);
      }
    }

    loadOptions();
  }, [token]);

  useEffect(() => {
    if (!mapElementRef.current || mapRef.current) {
      return;
    }

    const map = L.map(mapElementRef.current, {
      doubleClickZoom: false,
      zoomControl: true
    }).setView(TURKEY_CENTER, 6);

    const drawnItems = new L.FeatureGroup();
    drawnItemsRef.current = drawnItems;
    map.addLayer(drawnItems);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: "&copy; OpenStreetMap"
    }).addTo(map);

    map.on("click", handleMapClick);
    map.on("dblclick", finishPolygonDrawing);

    mapRef.current = map;
    window.setTimeout(() => map.invalidateSize(), 120);

    return () => {
      stopDrawing();
      stopEditing();
      map.remove();
      mapRef.current = null;
      activeLayerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const refreshId = window.setTimeout(() => {
      mapRef.current?.invalidateSize();
    }, 80);

    return () => window.clearTimeout(refreshId);
  }, [selectedId, targetType]);

  useEffect(() => {
    if (!selectedId) {
      setActiveRecord(null);
      setCurrentWkt("");
      clearMap();
      return;
    }

    async function loadGeographicPermission() {
      try {
        setMessage("");
        const records = await apiRequest(`/${selectedEndpoint}/${selectedId}`);
        const active = records.find((record) => record.isActive) ?? records[0] ?? null;
        setActiveRecord(active);
        setCurrentWkt(active?.wkt ?? "");
        drawWkt(active?.wkt);
      } catch (error) {
        setMessage(error.message);
      }
    }

    loadGeographicPermission();
  }, [selectedId, selectedEndpoint]);

  function stopDrawing() {
    isDrawingRef.current = false;
    mapElementRef.current?.classList.remove("geo-map-drawing");
  }

  function stopEditing() {
    if (activeEditRef.current) {
      activeEditRef.current.disable();
      activeEditRef.current = null;
    }
  }

  function clearMap() {
    stopDrawing();
    stopEditing();
    drawnItemsRef.current?.clearLayers();
    activeLayerRef.current = null;
    draftLatLngsRef.current = [];
    setHasPolygon(false);
  }

  function drawWkt(wkt) {
    const drawnItems = drawnItemsRef.current;

    if (!drawnItems) {
      return;
    }

    clearMap();
    const latLngs = wktToLatLngs(wkt);

    if (!latLngs.length) {
      mapRef.current?.setView(TURKEY_CENTER, 6);
      return;
    }

    const polygon = L.polygon(latLngs, { color: "#0d47a1", fillOpacity: 0.18, weight: 3 });
    drawnItems.addLayer(polygon);
    activeLayerRef.current = polygon;
    setHasPolygon(true);
    mapRef.current?.fitBounds(polygon.getBounds(), { padding: [22, 22], maxZoom: 12 });
    window.setTimeout(() => mapRef.current?.invalidateSize(), 80);
  }

  function startPolygonDrawing() {
    stopDrawing();
    stopEditing();
    drawnItemsRef.current?.clearLayers();
    activeLayerRef.current = null;
    draftLatLngsRef.current = [];
    setCurrentWkt("");
    setHasPolygon(false);
    setActiveRecord(null);
    isDrawingRef.current = true;
    mapElementRef.current?.classList.add("geo-map-drawing");
    setMessage("Haritada alanın köşelerine tıklayın. En az 3 nokta seçince Kaydet'e basabilirsiniz.");
  }

  function handleMapClick(event) {
    if (!isDrawingRef.current || activeEditRef.current) {
      return;
    }

    draftLatLngsRef.current = [...draftLatLngsRef.current, event.latlng];
    updateDraftPolygon();
  }

  function finishPolygonDrawing() {
    if (!isDrawingRef.current) {
      return;
    }

    if (draftLatLngsRef.current.length < 3) {
      setMessage("Polygon için en az 3 nokta seçin.");
      return;
    }

    stopDrawing();
    setMessage("Alan hazır. Kaydet ile yetkiyi uygulayabilirsiniz.");
  }

  function updateDraftPolygon() {
    const drawnItems = drawnItemsRef.current;
    const latLngs = draftLatLngsRef.current;

    if (!drawnItems) {
      return;
    }

    drawnItems.clearLayers();

    if (latLngs.length === 1) {
      const marker = L.circleMarker(latLngs[0], {
        radius: 5,
        color: "#0d47a1",
        fillColor: "#0d47a1",
        fillOpacity: 0.95,
        weight: 2
      });

      drawnItems.addLayer(marker);
      activeLayerRef.current = null;
      setHasPolygon(false);
      setCurrentWkt("");
      setMessage("1 nokta seçildi. Polygon için en az 3 nokta seçin.");
      return;
    }

    if (latLngs.length === 2) {
      const line = L.polyline(latLngs, { color: "#0d47a1", weight: 3 });
      drawnItems.addLayer(line);
      activeLayerRef.current = null;
      setHasPolygon(false);
      setCurrentWkt("");
      setMessage("2 nokta seçildi. Bir nokta daha seçince polygon hazır olur.");
      return;
    }

    const polygon = L.polygon(latLngs, { color: "#0d47a1", fillOpacity: 0.18, weight: 3 });
    drawnItems.addLayer(polygon);
    activeLayerRef.current = polygon;
    const wkt = latLngsToWkt(latLngs);
    setHasPolygon(true);
    setCurrentWkt(wkt);
    setMessage(`${latLngs.length} nokta seçildi. İsterseniz devam edin veya Kaydet'e basın.`);
  }

  function startEditing() {
    const map = mapRef.current;
    const drawnItems = drawnItemsRef.current;

    if (!map || !drawnItems || !activeLayerRef.current) {
      setMessage("Düzenlemek için önce kayıtlı veya çizilmiş bir alan olmalı.");
      return;
    }

    stopDrawing();
    stopEditing();
    setMessage("Köşe noktalarını sürükleyerek alanı düzenleyin, sonra Kaydet'e basın.");

    const editor = new L.EditToolbar.Edit(map, {
      featureGroup: drawnItems
    });

    activeEditRef.current = editor;
    editor.enable();
  }

  function applyCurrentLayerToWkt() {
    const layer = activeLayerRef.current ?? getFirstDrawnPolygonLayer();

    if (!layer) {
      return "";
    }

    activeLayerRef.current = layer;
    const wkt = latLngsToWkt(normalizePolygonLatLngs(layer.getLatLngs()));
    setCurrentWkt(wkt);
    return wkt;
  }

  function getFirstDrawnPolygonLayer() {
    const drawnItems = drawnItemsRef.current;

    if (!drawnItems) {
      return null;
    }

    let polygonLayer = null;

    drawnItems.eachLayer((layer) => {
      if (!polygonLayer && typeof layer.getLatLngs === "function") {
        polygonLayer = layer;
      }
    });

    return polygonLayer;
  }

  async function saveGeographicPermission() {
    const effectiveSelectedId = selectedId || selectedIdRef.current || selectElementRef.current?.value || "";
    const wkt = applyCurrentLayerToWkt() || currentWkt;

    if (!effectiveSelectedId) {
      setMessage(targetType === "user" ? "Önce kullanıcı seçin." : "Önce rol seçin.");
      return;
    }

    if (!wkt) {
      setMessage("Önce haritada polygon çizin.");
      return;
    }

    try {
      stopDrawing();
      stopEditing();
      const body = JSON.stringify({ wkt });

      if (activeRecord?.id) {
        await apiRequest(`/${selectedEndpoint}/${activeRecord.id}`, { method: "PUT", body });
      } else {
        await apiRequest(`/${selectedEndpoint}/${effectiveSelectedId}`, { method: "POST", body });
      }

      const records = await apiRequest(`/${selectedEndpoint}/${effectiveSelectedId}`);
      const active = records.find((record) => record.isActive) ?? records[0] ?? null;
      setActiveRecord(active);
      setCurrentWkt(active?.wkt ?? "");
      drawWkt(active?.wkt);
      setMessage(`${selectedLabel || effectiveSelectedId} için coğrafi yetki kaydedildi.`);
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function deleteGeographicPermission() {
    if (!activeRecord?.id) {
      return;
    }

    try {
      await apiRequest(`/${selectedEndpoint}/${activeRecord.id}`, { method: "DELETE" });
      setActiveRecord(null);
      setCurrentWkt("");
      clearMap();
      setMessage("Coğrafi yetki pasife alındı.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  function resetDraft() {
    setActiveRecord(null);
    setCurrentWkt("");
    clearMap();
    setMessage("Taslak temizlendi.");
  }

  function useTurkeyPermissionArea() {
    stopDrawing();
    stopEditing();
    setActiveRecord(null);
    setCurrentWkt(TURKEY_PERMISSION_WKT);
    drawWkt(TURKEY_PERMISSION_WKT);
    setMessage("Türkiye alanı hazır. Kaydet ile yetkiyi uygulayabilirsiniz.");
  }

  return (
    <div className="admin-crud">
      <div className="admin-crud-header">
        <h2>Coğrafi Yetki</h2>
      </div>

      {message && <div className="admin-message">{message}</div>}

      <div className="geo-toolbar">
        <div className="auth-switch geo-switch" aria-label="Coğrafi yetki tipi">
          {canManageUsers && (
            <button type="button" className={targetType === "user" ? "active-auth-mode" : ""} onClick={() => { setTargetType("user"); setSelectedId(""); }}>
              Kullanıcı
            </button>
          )}
          {canManageRoles && (
            <button type="button" className={targetType === "role" ? "active-auth-mode" : ""} onClick={() => { setTargetType("role"); setSelectedId(""); }}>
              Rol
            </button>
          )}
        </div>

        <select
          ref={selectElementRef}
          value={selectedId}
          onChange={(event) => {
            selectedIdRef.current = event.target.value;
            setSelectedId(event.target.value);
          }}
        >
          <option value="">{targetType === "user" ? "Kullanıcı seç" : "Rol seç"}</option>
          {selectedList.map((item) => (
            <option key={item.id} value={item.id}>{item.username ?? item.name}</option>
          ))}
        </select>

        <button type="button" className="secondary-button" disabled={!selectedId} onClick={startPolygonDrawing}>Çiz</button>
        <button type="button" className="secondary-button" disabled={!selectedId} onClick={useTurkeyPermissionArea}>Türkiye Alanı</button>
        <button type="button" className="secondary-button" disabled={!hasPolygon} onClick={startEditing}>Düzenle</button>
        <button type="button" className="secondary-button" onClick={resetDraft}>Temizle</button>
        <button type="button" className="primary-button" onClick={saveGeographicPermission}>Kaydet</button>
        <button type="button" className="danger-button" disabled={!activeRecord} onClick={deleteGeographicPermission}>Sil</button>
      </div>

      <div className="geo-map-shell">
        <div className="geo-map" ref={mapElementRef} />
      </div>
    </div>
  );
}

export default GeographicPermissionPanel;
