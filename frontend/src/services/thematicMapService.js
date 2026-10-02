const API_URL = "http://localhost:5291/api";
export async function getThematicMap(theme, extent, signal) {
  const paths = { NearestStopDistance: "nearest-stop-distance", WalkingTime: "walking-time", RouteDensity: "route-density" };
  const response = await fetch(`${API_URL}/thematic-map/${paths[theme]}`, { method:"POST", signal, headers:{ "Content-Type":"application/json", Authorization:"Bearer "+localStorage.getItem("token") }, body:JSON.stringify(extent) });
  const data = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(data.message || "Tematik analiz tamamlanamadı.");
  return data;
}
