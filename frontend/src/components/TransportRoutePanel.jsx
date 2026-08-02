import { useEffect, useState } from "react";
import { DndContext, PointerSensor, useSensor, useSensors } from "@dnd-kit/core";
import { arrayMove, SortableContext, useSortable, verticalListSortingStrategy } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { calculateRoute, createRoute, deleteRoute, getRouteById, getRouteSimulationStatus, getRoutes, reorderRouteStops, startRouteSimulation, stopRouteSimulation, updateRoute } from "../services/transportService";

const EMPTY_FORM = { name: "", color: "#2563EB", isActive: true };
const ROUTE_COLORS = ["#2563EB", "#4F46E5", "#7C3AED", "#DB2777", "#E11D48", "#DC2626", "#EA580C", "#D97706", "#16A34A", "#059669", "#0891B2", "#334155"];

function SortableStop({ stop, disabled }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: stop.id, disabled });
  return <li ref={setNodeRef} style={{ transform: CSS.Transform.toString(transform), transition }} className={isDragging ? "transport-stop dragging" : "transport-stop"}>
    <button type="button" className="drag-handle" disabled={disabled} {...attributes} {...listeners} aria-label={stop.name + " durağını sırala"}>⋮⋮</button>
    <span className="stop-sequence">{stop.sequenceNumber}</span><strong>{stop.name}</strong><span className={stop.isActive ? "status active" : "status"}>{stop.isActive ? "Aktif" : "Pasif"}</span>
  </li>;
}

export default function TransportRoutePanel({ userInfo, onClose, onAddStop, onRouteCalculated, onRouteSelected, onSimulationState, onToggleFollow, simulationState, followedRouteId, refreshKey }) {
  const permissions = new Set(userInfo?.permissions ?? []);
  const canView = permissions.has("TransportRoute.View");
  const canCreate = permissions.has("TransportRoute.Create");
  const canUpdate = permissions.has("TransportRoute.Update");
  const canDelete = permissions.has("TransportRoute.Delete");
  const canCalculate = permissions.has("TransportRoute.Calculate");
  const canStartSimulation = permissions.has("TransportSimulation.Start");
  const canStopSimulation = permissions.has("TransportSimulation.Stop");
  const [routes, setRoutes] = useState([]);
  const [detail, setDetail] = useState(null);
  const [mode, setMode] = useState("list");
  const [form, setForm] = useState(EMPTY_FORM);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [simulationStatus, setSimulationStatus] = useState(null);
  const [simulationLoading, setSimulationLoading] = useState(false);
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  async function loadRoutes() { setLoading(true); setError(""); try { setRoutes(await getRoutes()); } catch (e) { setError(e.message); } finally { setLoading(false); } }
  useEffect(() => { if (canView) loadRoutes(); else setLoading(false); }, [canView]);
  useEffect(() => { if (!refreshKey || !canView) return; loadRoutes(); if (detail?.id) getRouteById(detail.id).then(setDetail).catch(e => setError(e.message)); }, [refreshKey]);

  async function openDetail(id) {
    setLoading(true); setError("");
    try {
      const [route, status] = await Promise.all([getRouteById(id), getRouteSimulationStatus(id)]);
      setDetail(route); setSimulationStatus(status); onSimulationState?.(status.simulation); onRouteSelected?.(route.id); setMode("detail");
    } catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }
  function openCreate() { setForm(EMPTY_FORM); setError(""); setMode("create"); }
  function openEdit(route) { setForm({ name: route.name, color: route.color, isActive: route.isActive }); setDetail(route); setError(""); setMode("edit"); }
  async function submit(event) {
    event.preventDefault(); const name = form.name.trim(); if (!name) return setError("Güzergah adı zorunludur."); if (!/^#[0-9a-f]{6}$/i.test(form.color)) return setError("Geçerli bir HEX renk girin.");
    setSaving(true); setError(""); try { if (mode === "create") { await createRoute({ ...form, name }); setMessage("Güzergah başarıyla kaydedildi."); } else { await updateRoute(detail.id, { ...form, name }); setMessage("Güzergah başarıyla güncellendi."); } setMode("list"); setDetail(null); await loadRoutes(); } catch (e) { setError(e.message); } finally { setSaving(false); }
  }
  async function confirmDelete() { setSaving(true); setError(""); try { await deleteRoute(deleteTarget.id); setDeleteTarget(null); setDetail(null); setMode("list"); setMessage("Güzergah başarıyla silindi."); await loadRoutes(); } catch (e) { setError(e.message); } finally { setSaving(false); } }
  async function dragEnd({ active, over }) {
    if (!over || active.id === over.id || !detail) return;
    const oldStops = detail.stops; const oldIndex = oldStops.findIndex(x => x.id === active.id); const newIndex = oldStops.findIndex(x => x.id === over.id);
    const reordered = arrayMove(oldStops, oldIndex, newIndex).map((x, index) => ({ ...x, sequenceNumber: index + 1 })); setDetail({ ...detail, stops: reordered }); setError("");
    try {
      const result = await reorderRouteStops(detail.id, reordered.map(x => x.id));
      if (result?.route) onRouteCalculated?.({ ...result.route, color: detail.color, name: detail.name });
      setDetail(await getRouteById(detail.id));
      setMessage(result?.routeCalculationStatus === "Failed" ? "Sıralama kaydedildi; rota yeniden hesaplanamadı." : "Sıralama kaydedildi ve rota hesaplandı.");
      if (result?.routeCalculationError) setError(result.routeCalculationError);
    } catch (e) { setDetail({ ...detail, stops: oldStops }); setError(e.message); }
  }
  async function calculate() {
    if (!detail || detail.stops.filter(x => x.isActive).length < 2) return;
    setSaving(true); setError(""); setMessage("");
    try {
      const result = await calculateRoute(detail.id);
      onRouteCalculated?.({ ...result, color: detail.color, name: detail.name });
      setDetail(await getRouteById(detail.id));
      setMessage("Rota başarıyla hesaplandı.");
    } catch (e) { setError(e.message); }
    finally { setSaving(false); }
  }
  async function startSimulation() {
    if (!detail) return;
    setSimulationLoading(true); setError(""); setMessage("");
    try {
      const response = await startRouteSimulation(detail.id);
      setSimulationStatus({ routeId: detail.id, isRunning: true, canFollow: true, status: response.simulation.status, simulation: response.simulation });
      onSimulationState?.(response.simulation);
      setMessage("Araç simülasyonu başlatıldı.");
    } catch (e) { setError(e.message); }
    finally { setSimulationLoading(false); }
  }
  async function stopSimulation() {
    if (!detail) return;
    setSimulationLoading(true); setError(""); setMessage("");
    try {
      await stopRouteSimulation(detail.id);
      const status = await getRouteSimulationStatus(detail.id);
      setSimulationStatus(status); onSimulationState?.(status.simulation);
      setMessage("Simülasyon durdurma isteği gönderildi.");
    } catch (e) { setError(e.message); }
    finally { setSimulationLoading(false); }
  }
  const liveSimulation = simulationState?.routeId === detail?.id ? simulationState : simulationStatus?.simulation;
  const simulationRunning = liveSimulation?.isRunning ?? simulationStatus?.isRunning ?? false;
  const isFollowing = followedRouteId === detail?.id;

  if (!canView) return <aside className="transport-panel"><div className="transport-header"><h2>Güzergah Yönetimi</h2><button onClick={onClose}>×</button></div><p className="transport-error">Bu panele erişim yetkiniz yok.</p></aside>;
  return <aside className="transport-panel">
    <div className="transport-header"><div><small>Ulaşım Modülü</small><h2>Güzergah Yönetimi</h2></div><button type="button" onClick={onClose}>×</button></div>
    {message && <div className="transport-success">{message}</div>}{error && <div className="transport-error">{error}</div>}
    {loading ? <div className="transport-empty">Yükleniyor…</div> : (mode === "create" || mode === "edit") ? <form className="transport-form" onSubmit={submit}>
      <h3>{mode === "create" ? "Yeni Güzergah" : "Güzergahı Düzenle"}</h3><label>Güzergah adı<input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></label>
      <label>Güzergah rengi<div className="route-color-picker"><div className="route-color-preview" style={{background:`linear-gradient(135deg, ${form.color}, ${form.color}bb)`}}><span style={{background:form.color}}/><strong>{form.color}</strong></div><div className="route-color-swatches">{ROUTE_COLORS.map(color => <button type="button" key={color} className={form.color.toUpperCase() === color ? "selected" : ""} style={{background:color}} onClick={() => setForm({...form,color})} aria-label={color + " rengini seç"}/>)}</div><div className="route-color-custom"><input type="color" value={/^#[0-9a-f]{6}$/i.test(form.color) ? form.color : "#2563EB"} onChange={e => setForm({ ...form, color: e.target.value.toUpperCase() })}/><input value={form.color} maxLength="7" onChange={e => setForm({ ...form, color: e.target.value.toUpperCase() })}/></div></div></label>
      {mode === "edit" && <label className="transport-check"><input type="checkbox" checked={form.isActive} onChange={e => setForm({ ...form, isActive: e.target.checked })}/> Aktif</label>}
      <div className="transport-actions"><button type="button" className="secondary-button" onClick={() => setMode("list")}>Vazgeç</button><button className="primary-button" disabled={saving}>{saving ? "Kaydediliyor…" : "Kaydet"}</button></div>
    </form> : mode === "detail" && detail ? <div className="transport-detail">
      <button type="button" className="transport-back" onClick={() => { setMode("list"); onRouteSelected?.(null); }}>← Listeye dön</button><div className="route-title"><span style={{ background: detail.color }}/><div><h3>{detail.name}</h3><small>{detail.color} · {detail.stopCount} durak</small></div></div>
      <div className="transport-actions">{canUpdate && <button className="secondary-button" onClick={() => openEdit(detail)}>Düzenle</button>}{canCalculate && <button className="primary-button" onClick={calculate} disabled={saving || detail.stops.filter(x => x.isActive).length < 2} title={detail.stops.filter(x => x.isActive).length < 2 ? "En az iki aktif durak gereklidir." : ""}>{saving ? "Hesaplanıyor…" : "Rotayı Hesapla"}</button>}{permissions.has("TransportStop.Create") && <button className="primary-button" onClick={() => onAddStop({ id: detail.id, name: detail.name, color: detail.color })}>Güzergaha Durak Ekle</button>}</div>
      <div className="transport-actions simulation-actions">
        {canStartSimulation && <button className="primary-button" onClick={startSimulation} disabled={simulationLoading || simulationRunning}>{simulationLoading ? "İşleniyor…" : "Simülasyonu Başlat"}</button>}
        {canStopSimulation && <button className="danger-button" onClick={stopSimulation} disabled={simulationLoading || !simulationRunning}>Simülasyonu Durdur</button>}
        <button className="secondary-button" onClick={() => onToggleFollow?.(detail, !isFollowing)} disabled={!isFollowing && !simulationRunning}>{isFollowing ? "Takibi Bırak" : "Takip Et"}</button>
      </div>
      <small>Simülasyon: {liveSimulation?.status ?? simulationStatus?.status ?? "Başlatılmadı"}{liveSimulation?.progressPercent != null ? ` · %${Number(liveSimulation.progressPercent).toFixed(1)}` : ""}</small>
      {detail.routeCalculationStatus && <small>Rota durumu: {detail.routeCalculationStatus}{detail.distanceMeters != null ? ` · ${(detail.distanceMeters / 1000).toFixed(1)} km` : ""}{detail.durationSeconds != null ? ` · ${Math.round(detail.durationSeconds / 60)} dk` : ""}</small>}
      <h3>Duraklar</h3>{detail.stops.length === 0 ? <div className="transport-empty">Durak bulunamadı.</div> : <DndContext sensors={sensors} onDragEnd={dragEnd}><SortableContext items={detail.stops.map(x => x.id)} strategy={verticalListSortingStrategy}><ol className="transport-stops">{detail.stops.map(stop => <SortableStop key={stop.id} stop={stop} disabled={!canUpdate}/>)}</ol></SortableContext></DndContext>}
    </div> : <div className="transport-list">
      {canCreate && <button type="button" className="primary-button new-route" onClick={openCreate}>+ Yeni Güzergah</button>}
      {routes.length === 0 ? <div className="transport-empty">Henüz aktif güzergah bulunmuyor.</div> : routes.map(route => <article className="route-card" key={route.id}><div className="route-title"><span style={{ background: route.color }}/><div><h3>{route.name}</h3><small>{route.color} · {route.stopCount} durak · {route.isActive ? "Aktif" : "Pasif"}</small></div></div><div className="route-card-actions"><button onClick={() => openDetail(route.id)}>Detay</button>{canUpdate && <button onClick={() => openEdit(route)}>Düzenle</button>}{canDelete && <button className="delete-link" onClick={() => setDeleteTarget(route)}>Sil</button>}</div></article>)}
    </div>}
    {deleteTarget && <div className="transport-confirm"><div><h3>Güzergah silinsin mi?</h3><p><strong>{deleteTarget.name}</strong> ve durakları pasife alınacak.</p><div className="transport-actions"><button className="secondary-button" onClick={() => setDeleteTarget(null)}>Vazgeç</button><button className="danger-button" disabled={saving} onClick={confirmDelete}>Sil</button></div></div></div>}
  </aside>;
}
