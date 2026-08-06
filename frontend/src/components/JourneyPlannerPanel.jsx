import { useMemo, useState } from "react";
import { planJourney } from "../services/transportService";

const WALKING_OPTIONS = [500, 1000, 1500, 2500, 5000];

function formatDuration(seconds) {
  const total = Math.max(0, Math.round(Number(seconds) || 0));
  const hours = Math.floor(total / 3600), minutes = Math.floor((total % 3600) / 60), remaining = total % 60;
  if (hours) return `${hours} sa${minutes ? ` ${minutes} dk` : ""}`;
  return `${minutes} dk${remaining ? ` ${remaining} sn` : ""}`;
}

function formatDistance(meters) {
  const value = Number(meters) || 0;
  return value >= 1000 ? `${(value / 1000).toFixed(value >= 10000 ? 0 : 1)} km` : `${Math.round(value)} m`;
}

function CoordinateFields({ title, point, mode, onChange, onSelect }) {
  return <section className="journey-coordinate-group">
    <div className="journey-field-title"><strong>{title}</strong><button type="button" className={mode ? "active" : ""} onClick={onSelect}>{mode ? "Seçim aktif" : "Haritadan seç"}</button></div>
    <div className="journey-coordinate-inputs">
      <label>Longitude<input type="number" step="any" min="-180" max="180" value={point?.longitude ?? ""} onChange={event => onChange({ ...point, longitude: event.target.value })}/></label>
      <label>Latitude<input type="number" step="any" min="-90" max="90" value={point?.latitude ?? ""} onChange={event => onChange({ ...point, latitude: event.target.value })}/></label>
    </div>
  </section>;
}

export default function JourneyPlannerPanel({ canPlan, startPoint, destinationPoint, selectionMode,
  onStartPointChange, onDestinationPointChange, onSelectionMode, onResult, onClear, onClose,
  onFocusStep, onShowAll }) {
  const [maximumWalkingDistance, setMaximumWalkingDistance] = useState("1500");
  const [loading, setLoading] = useState(false);
  const [journeyResult, setJourneyResult] = useState(null);
  const [selectedStep, setSelectedStep] = useState(null);
  const [error, setError] = useState("");

  const validationError = useMemo(() => {
    const startLongitude = Number(startPoint?.longitude), startLatitude = Number(startPoint?.latitude);
    const destinationLongitude = Number(destinationPoint?.longitude), destinationLatitude = Number(destinationPoint?.latitude);
    const walking = Number(maximumWalkingDistance);
    if (!startPoint || !destinationPoint || [startLongitude, startLatitude, destinationLongitude, destinationLatitude].some(value => !Number.isFinite(value))) return "Başlangıç ve hedef seçilmelidir.";
    if (Math.abs(startLongitude) > 180 || Math.abs(destinationLongitude) > 180 || Math.abs(startLatitude) > 90 || Math.abs(destinationLatitude) > 90) return "Koordinatlar geçerli aralıkta olmalıdır.";
    if (startLongitude === destinationLongitude && startLatitude === destinationLatitude) return "Başlangıç ve hedef aynı olamaz.";
    if (!Number.isFinite(walking) || walking < 100 || walking > 5000) return "Yürüme mesafesi 100–5000 metre arasında olmalıdır.";
    return "";
  }, [startPoint, destinationPoint, maximumWalkingDistance]);

  async function submit(event) {
    event.preventDefault();
    if (validationError || loading) return;
    setLoading(true); setError(""); setSelectedStep(null);
    try {
      const result = await planJourney({
        startLongitude: Number(startPoint.longitude), startLatitude: Number(startPoint.latitude),
        destinationLongitude: Number(destinationPoint.longitude), destinationLatitude: Number(destinationPoint.latitude),
        maximumWalkingDistanceMeters: Number(maximumWalkingDistance)
      });
      setJourneyResult(result);
      onResult(result);
    } catch (requestError) {
      setJourneyResult(null); onResult(null); setError(requestError.message);
    } finally { setLoading(false); }
  }

  function clear() { setJourneyResult(null); setSelectedStep(null); setError(""); onClear(); }
  if (!canPlan) return <aside className="journey-panel"><div className="journey-header"><h2>Yolculuk Rehberi</h2><button onClick={onClose}>×</button></div><div className="transport-error">Bu özelliği kullanma yetkiniz yok.</div></aside>;
  const steps = [...(journeyResult?.steps ?? [])].sort((a, b) => a.order - b.order);
  return <aside className="journey-panel">
    <div className="journey-header"><div><small>Yol Tarifi</small><h2>Yolculuk Rehberi</h2></div><button type="button" onClick={onClose}>×</button></div>
    <form className="journey-form" onSubmit={submit}>
      <CoordinateFields title="Başlangıç" point={startPoint} mode={selectionMode === "start"} onChange={onStartPointChange} onSelect={() => onSelectionMode(selectionMode === "start" ? null : "start")}/>
      <CoordinateFields title="Hedef" point={destinationPoint} mode={selectionMode === "destination"} onChange={onDestinationPointChange} onSelect={() => onSelectionMode(selectionMode === "destination" ? null : "destination")}/>
      <label className="journey-walking-limit">Maksimum yürüme mesafesi <span>metre</span><input type="number" min="100" max="5000" step="50" list="walking-distances" value={maximumWalkingDistance} onChange={event => setMaximumWalkingDistance(event.target.value)}/><datalist id="walking-distances">{WALKING_OPTIONS.map(value => <option key={value} value={value}/>)}</datalist></label>
      {error && <div className="transport-error">{error}</div>}
      <div className="journey-form-actions"><button type="button" className="secondary-button" onClick={clear}>Temizle</button><button className="primary-button" disabled={Boolean(validationError) || loading}>{loading ? "Plan oluşturuluyor…" : "Plan Oluştur"}</button></div>
      {validationError && (startPoint || destinationPoint) && <small className="journey-validation">{validationError}</small>}
    </form>
    {journeyResult && !journeyResult.found && <div className="journey-not-found"><strong>{journeyResult.message}</strong>{journeyResult.suggestions?.length > 0 && <ul>{journeyResult.suggestions.map(item => <li key={item}>{item}</li>)}</ul>}</div>}
    {journeyResult?.found && <div className="journey-result">
      <div className="journey-summary">
        <div><span>Toplam Süre</span><strong>{formatDuration(journeyResult.totalDurationSeconds)}</strong></div><div><span>Mesafe</span><strong>{formatDistance(journeyResult.totalDistanceMeters)}</strong></div>
        <div><span>Yürüme</span><strong>{formatDistance(journeyResult.totalWalkingDistanceMeters)}</strong></div><div><span>Durak</span><strong>{journeyResult.transitStopCount}</strong></div>
      </div>
      <div className="journey-route-badge" style={{ borderLeftColor: journeyResult.routeColor || "#2563eb" }}><i style={{ background: journeyResult.routeColor || "#2563eb" }}/><span>Kullanılacak güzergah</span><strong>{journeyResult.routeName}</strong></div>
      <button type="button" className="secondary-button journey-show-all" onClick={onShowAll}>Planın Tamamını Göster</button>
      <div className="journey-steps">{steps.map(step => {
        const transit = step.type === "RideTransit";
        return <button type="button" key={step.order} className={selectedStep === step.order ? "journey-step selected" : "journey-step"} style={transit ? { borderLeftColor: step.routeColor || journeyResult.routeColor } : undefined} onClick={() => { setSelectedStep(step.order); onFocusStep(step.order); }}>
          <span className={transit ? "journey-step-icon transit" : "journey-step-icon"}>{transit ? "▣" : "●"}</span><span className="journey-step-content"><strong>{step.title}</strong><small>{step.description}</small>{transit && <small>{journeyResult.boardingStop?.stopName} → {journeyResult.alightingStop?.stopName} · {step.stopCount ?? journeyResult.transitStopCount} durak</small>}<span>{formatDistance(step.distanceMeters)} · {formatDuration(step.durationSeconds)}{step.isEstimated ? <em>Tahmini</em> : null}</span></span>
        </button>;
      })}</div>
      {journeyResult.warnings?.map(warning => <div className="journey-warning" key={warning}>ⓘ {warning}</div>)}
    </div>}
  </aside>;
}
