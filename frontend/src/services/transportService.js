const API_URL = "http://localhost:5291/api";

async function request(path, options = {}) {
  const response = await fetch(API_URL + path, {
    ...options,
    headers: { "Content-Type": "application/json", Authorization: "Bearer " + localStorage.getItem("token"), ...options.headers }
  });
  if (response.status === 204) return null;
  const contentType = response.headers.get("content-type") || "";
  const data = contentType.includes("application/json") ? await response.json() : await response.text();
  if (!response.ok) throw new Error(typeof data === "string" ? data : data.message || data.title || "İşlem tamamlanamadı.");
  return data;
}

export const getRoutes = () => request("/transport/routes");
export const getRouteById = (id) => request("/transport/routes/" + id);
export const createRoute = (route) => request("/transport/routes", { method: "POST", body: JSON.stringify(route) });
export const updateRoute = (id, route) => request("/transport/routes/" + id, { method: "PUT", body: JSON.stringify(route) });
export const deleteRoute = (id) => request("/transport/routes/" + id, { method: "DELETE" });
export const calculateRoute = (id) => request("/transport/routes/" + id + "/calculate", { method: "POST" });
export const reorderRouteStops = (routeId, stopIds) => request("/transport-routes/" + routeId + "/reorder-stops", { method: "PUT", body: JSON.stringify({ stopIds }) });
export const getStops = (routeId) => request("/transport/stops" + (routeId ? "?routeId=" + routeId : ""));
export const createStop = (stop) => request("/transport/stops", { method: "POST", body: JSON.stringify(stop) });
export const updateStop = (id, stop) => request("/transport/stops/" + id, { method: "PUT", body: JSON.stringify(stop) });
export const deleteStop = (id) => request("/transport/stops/" + id, { method: "DELETE" });
export const startRouteSimulation = (routeId, speedKmh) => request("/transport/routes/" + routeId + "/simulation/start", { method: "POST", body: JSON.stringify({ speedKmh }) });
export const stopRouteSimulation = (routeId) => request("/transport/routes/" + routeId + "/simulation/stop", { method: "POST" });
export const updateRouteSimulationSpeed = (routeId, speedKmh) => request("/transport/routes/" + routeId + "/simulation/speed", { method: "PUT", body: JSON.stringify({ speedKmh }) });
export const getRouteSimulationStatus = (routeId) => request("/transport/routes/" + routeId + "/simulation/status");
export const planJourney = (payload) => request("/journey-planner/plan", { method: "POST", body: JSON.stringify(payload) });
