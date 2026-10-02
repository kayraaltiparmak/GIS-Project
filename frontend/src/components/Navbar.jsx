function formatTime(totalSeconds) {
  const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return minutes + ":" + seconds;
}

function GeometryIcon({ type }) {
  if (type === "Point") {
    return (
      <svg className="nav-tool-icon" viewBox="0 0 20 20" aria-hidden="true">
        <circle cx="10" cy="10" r="4" />
      </svg>
    );
  }

  if (type === "Line") {
    return (
      <svg className="nav-tool-icon" viewBox="0 0 20 20" aria-hidden="true">
        <path d="M4 15 9 6l7 7" />
        <circle cx="4" cy="15" r="1.5" />
        <circle cx="9" cy="6" r="1.5" />
        <circle cx="16" cy="13" r="1.5" />
      </svg>
    );
  }

  return (
    <svg className="nav-tool-icon" viewBox="0 0 20 20" aria-hidden="true">
      <path d="m4 6 6-3 6 4-2 9H6L4 6Z" />
    </svg>
  );
}

function HeatmapIcon() {
  return (
    <svg className="nav-tool-icon heatmap-tool-icon" viewBox="0 0 20 20" aria-hidden="true">
      <path d="M5 16c-2-2.3-.5-4.1.8-5.5 1.1-1.2 1.8-2.3 1.1-4.2 2.8 1.1 3.7 3 3.2 5.1 1.5-.8 2.1-2.1 1.8-3.9 2.2 1.7 3.3 3.6 3.1 5.5-.2 2.2-2 4-5 4-2.2 0-3.8-.4-5-1Z" />
      <path d="M8.2 16.8c-1.1-1-.8-2.2.2-3.1.8-.7 1.2-1.5 1-2.5 1.9 1.1 3 2.5 2.5 4-.3 1-1.2 1.7-2.5 1.8" />
    </svg>
  );
}

function Navbar({
  activeTool,
  isHeatmapActive,
  userInfo,
  remainingSeconds,
  onAdminOpen,
  onLogout,
  onStartDrawing,
  onToggleHeatmap,
  onLocationAnalysis,
  onTransportRoutes,
  onJourneyPlanner
  ,onThematicMap
}) {
  const permissions = new Set(userInfo?.permissions ?? []);
  const canOpenAdminPanel =
    userInfo?.isAdmin ||
    permissions.has("User.Manage") ||
    permissions.has("Role.Manage") ||
    permissions.has("Permission.Manage");

  return (
    <header className="app-navbar">
      <div className="navbar-brand">
        <span className="topbar-kicker">OpenLayers</span>
        <h1>Dünya Haritası</h1>
      </div>

      <nav className="navbar-actions" aria-label="Ana menü">
        {permissions.has("Poi.Create") && (
          <button type="button" className={activeTool === "Poi" ? "active-nav-tool" : ""} onClick={() => onStartDrawing("Poi")}>POI Ekle</button>
        )}
        {permissions.has("Point.Create") && (
          <button
            type="button"
            className={activeTool === "Point" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Point")}
          >
            <GeometryIcon type="Point" />
            Point
          </button>
        )}
        {permissions.has("Line.Create") && (
          <button
            type="button"
            className={activeTool === "Line" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Line")}
          >
            <GeometryIcon type="Line" />
            Line
          </button>
        )}
        {permissions.has("Polygon.Create") && (
          <button
            type="button"
            className={activeTool === "Polygon" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Polygon")}
          >
            <GeometryIcon type="Polygon" />
            Polygon
          </button>
        )}
        {permissions.has("LocationAnalysis.Use") && (
          <button type="button" onClick={onLocationAnalysis}>Konum Analizi</button>
        )}
        {["TransportRoute.View", "TransportRoute.Create", "TransportRoute.Update", "TransportRoute.Delete"].some(code => permissions.has(code)) && (
          <button type="button" onClick={onTransportRoutes}>Güzergah Yönetimi</button>
        )}
        {permissions.has("TransportStop.Create") && <button type="button" className={activeTool === "TransportStop" ? "active-nav-tool" : ""} onClick={() => onStartDrawing("TransportStop")}>Durak Ekle</button>}
        {permissions.has("TransportJourneyPlan.View") && <button type="button" onClick={onJourneyPlanner}>Yol Tarifi</button>}
        {permissions.has("ThematicMap.View") && <button type="button" onClick={onThematicMap}>Tematik Harita</button>}
        {permissions.has("Inventory.Analyze") && (
          <button
            type="button"
            className={activeTool === "InventoryAnalysis" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("InventoryAnalysis")}
          >
            Envanter Analizi
          </button>
        )}
        {permissions.has("Heatmap.Analyze") && (
          <button
            type="button"
            className={isHeatmapActive ? "active-nav-tool" : ""}
            onClick={onToggleHeatmap}
          >
            <HeatmapIcon />
            Isı Haritası Analizi
          </button>
        )}
        {canOpenAdminPanel && (
          <button type="button" onClick={onAdminOpen}>
            Admin Paneli
          </button>
        )}
        <button type="button" className="close-tool" onClick={onLogout}>
          Close
        </button>
        <div className="session-timer">{formatTime(remainingSeconds)}</div>
      </nav>
    </header>
  );
}

export default Navbar;
