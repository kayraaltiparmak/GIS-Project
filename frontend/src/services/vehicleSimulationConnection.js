import * as signalR from "@microsoft/signalr";

const HUB_URL = "http://localhost:5291/hubs/vehicle-simulation";

export const VEHICLE_SIMULATION_EVENTS = Object.freeze({
  positionUpdated: "VehiclePositionUpdated",
  started: "SimulationStarted",
  completed: "SimulationCompleted",
  stopped: "SimulationStopped",
  failed: "SimulationFailed",
  speedChanged: "SimulationSpeedChanged"
});

let connection;
let startPromise;
let joinedRouteId = null;

function getConnection() {
  if (connection) return connection;
  connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, { accessTokenFactory: () => localStorage.getItem("token") || "" })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();
  connection.onreconnected(async () => {
    if (joinedRouteId != null) {
      try { await connection.invoke("JoinRouteGroup", joinedRouteId); }
      catch (error) { console.error("Route simulation group could not be restored.", error); }
    }
  });
  return connection;
}

export async function ensureVehicleSimulationConnection() {
  const current = getConnection();
  if (current.state === signalR.HubConnectionState.Connected) return current;
  if (startPromise) return startPromise;
  startPromise = current.start().then(() => current).finally(() => { startPromise = null; });
  return startPromise;
}

export async function joinVehicleRouteGroup(routeId) {
  const numericRouteId = Number(routeId);
  if (!Number.isInteger(numericRouteId) || numericRouteId <= 0) throw new Error("Geçerli bir güzergah seçilmelidir.");
  const current = await ensureVehicleSimulationConnection();
  if (joinedRouteId === numericRouteId) return;
  if (joinedRouteId != null) {
    try { await current.invoke("LeaveRouteGroup", joinedRouteId); } catch { /* reconnect handles stale membership */ }
  }
  await current.invoke("JoinRouteGroup", numericRouteId);
  joinedRouteId = numericRouteId;
}

export async function leaveVehicleRouteGroup(routeId) {
  const numericRouteId = Number(routeId);
  if (joinedRouteId !== numericRouteId) return;
  const current = getConnection();
  if (current.state === signalR.HubConnectionState.Connected)
    await current.invoke("LeaveRouteGroup", numericRouteId);
  joinedRouteId = null;
}

export function subscribeVehicleSimulation(eventName, handler) {
  const current = getConnection();
  current.on(eventName, handler);
  return () => current.off(eventName, handler);
}
