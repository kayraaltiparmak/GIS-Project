function formatTime(totalSeconds) {
  const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return minutes + ":" + seconds;
}

function Navbar({
  activeTool,
  isHeatmapActive,
  userInfo,
  remainingSeconds,
  onAdminOpen,
  onLogout,
  onStartDrawing,
  onToggleHeatmap
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
        {permissions.has("Point.Create") && (
          <button
            type="button"
            className={activeTool === "Point" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Point")}
          >
            Point
          </button>
        )}
        {permissions.has("Line.Create") && (
          <button
            type="button"
            className={activeTool === "Line" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Line")}
          >
            Line
          </button>
        )}
        {permissions.has("Polygon.Create") && (
          <button
            type="button"
            className={activeTool === "Polygon" ? "active-nav-tool" : ""}
            onClick={() => onStartDrawing("Polygon")}
          >
            Polygon
          </button>
        )}
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
