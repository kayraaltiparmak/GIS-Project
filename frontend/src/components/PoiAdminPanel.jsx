import { useEffect, useMemo, useState } from "react";

const API = "http://localhost:5291/api";
const emptyCategory = { id: null, name: "", description: "", iconKey: "default", parentId: "", isActive: true };
const iconOptions = [{key:"default",label:"Standart"},{key:"pharmacy",label:"Eczane (E)"},{key:"cafe",label:"Kafe (Fincan)"},{key:"restaurant",label:"Restoran"},{key:"hospital",label:"Hastane"},{key:"school",label:"Okul"},{key:"market",label:"Market"}];
const emptyPoi = { id: null, name: "", categoryId: "", workingHours: "", wkt: "", isActive: true };

export default function PoiAdminPanel({ token, userInfo }) {
  const permissions = useMemo(() => new Set(userInfo?.permissions ?? []), [userInfo]);
  const canManagePois = permissions.has("Poi.Manage");
  const canUpdatePoi = canManagePois || permissions.has("Poi.Update");
  const canDeletePoi = canManagePois || permissions.has("Poi.Delete");
  const canManageCategories = permissions.has("PoiCategory.Manage");
  const [tab, setTab] = useState("pois");
  const [pois, setPois] = useState([]), [categories, setCategories] = useState([]), [parents, setParents] = useState([]);
  const [categoryForm, setCategoryForm] = useState(emptyCategory), [poiForm, setPoiForm] = useState(emptyPoi), [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(false), [error, setError] = useState(""), [message, setMessage] = useState("");

  async function request(path, options = {}) {
    const response = await fetch(API + path, { ...options, headers: { Authorization: "Bearer " + token, "Content-Type": "application/json" } });
    if (!response.ok) throw new Error((await response.text()) || (response.status === 403 ? "Bu işlem için yetkiniz yok." : "İşlem tamamlanamadı."));
    return response.status === 204 ? null : response.json();
  }
  async function loadPois() {
    if (!canManagePois) return;
    setLoading(true); setError("");
    try { const [items, active] = await Promise.all([request("/pois?includeDeleted=true"), request("/poi-categories/active")]); setPois(items); setParents(active); }
    catch (e) { setError(e.message); } finally { setLoading(false); }
  }
  async function loadCategories() {
    if (!canManageCategories) return;
    setLoading(true); setError("");
    try { const [items, active] = await Promise.all([request("/poi-categories?includeDeleted=true"), request("/poi-categories/active")]); setCategories(items); setParents(active); }
    catch (e) { setError(e.message); } finally { setLoading(false); }
  }
  useEffect(() => { if (tab === "pois") loadPois(); else loadCategories(); }, [tab, token, canManagePois, canManageCategories]);

  async function showDetail(id) { try { setDetail(await request("/pois/" + id)); } catch (e) { setError(e.message); } }
  function editPoi(poi) { setPoiForm({ id:poi.id, name:poi.name, categoryId:poi.categoryId, workingHours:poi.workingHours, wkt:poi.wkt, isActive:poi.isActive }); setDetail(null); }
  async function savePoi(event) {
    event.preventDefault();
    try { await request("/pois/" + poiForm.id, { method:"PUT", body:JSON.stringify({ ...poiForm, categoryId:Number(poiForm.categoryId) }) }); setPoiForm(emptyPoi); setMessage("POI güncellendi."); await loadPois(); }
    catch (e) { setError(e.message); }
  }
  async function deletePoi(id) {
    if (!window.confirm("POI kaydı soft delete ile pasife alınacak. Devam edilsin mi?")) return;
    try { await request("/pois/" + id, { method:"DELETE" }); setDetail(null); setMessage("POI silindi ve pasife alındı."); await loadPois(); }
    catch (e) { setError(e.message); }
  }
  async function saveCategory(event) {
    event.preventDefault();
    try { const payload={ name:categoryForm.name, description:categoryForm.description, iconKey:categoryForm.iconKey, parentId:categoryForm.parentId?Number(categoryForm.parentId):null, isActive:categoryForm.isActive }; await request("/poi-categories"+(categoryForm.id?"/"+categoryForm.id:""),{method:categoryForm.id?"PUT":"POST",body:JSON.stringify(payload)}); setCategoryForm(emptyCategory); setError(""); setMessage("Kategori kaydedildi."); await loadCategories(); }
    catch(e){setError(e.message)}
  }
  async function deleteCategory(id) {
    if (!window.confirm("Kategori soft delete ile pasife alınacak. Devam edilsin mi?")) return;
    try { await request("/poi-categories/"+id,{method:"DELETE"}); setMessage("Kategori pasife alındı."); await loadCategories(); }
    catch(e){setError(e.message)}
  }
  const date = value => new Date(value).toLocaleString("tr-TR");

  return <div className="admin-crud poi-admin">
    <div className="poi-tabs"><button className={tab==="pois"?"active-admin-tab":""} onClick={()=>setTab("pois")}>POI Listesi</button><button className={tab==="categories"?"active-admin-tab":""} onClick={()=>setTab("categories")}>Kategori Yönetimi</button></div>
    {message&&<div className="admin-message">{message}</div>}{error&&<div className="admin-error">{error}</div>}
    {tab==="pois" ? (!canManagePois ? <Forbidden text="POI listesini yönetmek için Poi.Manage yetkisi gerekir."/> : <>
      {poiForm.id&&<form className="admin-form poi-edit-form" onSubmit={savePoi}><input required value={poiForm.name} onChange={e=>setPoiForm({...poiForm,name:e.target.value})} placeholder="POI adı"/><select required value={poiForm.categoryId} onChange={e=>setPoiForm({...poiForm,categoryId:e.target.value})}><option value="">Kategori seçin</option>{parents.map(c=><option key={c.id} value={c.id}>{c.displayName}</option>)}</select><input value={poiForm.workingHours} onChange={e=>setPoiForm({...poiForm,workingHours:e.target.value})} placeholder="Mesai saatleri"/><label className="admin-check"><input type="checkbox" checked={poiForm.isActive} onChange={e=>setPoiForm({...poiForm,isActive:e.target.checked})}/> Aktif</label><button className="primary-button">Güncelle</button><button type="button" onClick={()=>setPoiForm(emptyPoi)}>Vazgeç</button></form>}
      {loading?<div className="admin-state">POI kayıtları yükleniyor…</div>:pois.length===0?<div className="admin-state">Henüz POI kaydı bulunmuyor.</div>:<table className="admin-table"><thead><tr><th>POI adı</th><th>Kategori</th><th>Üst kategori</th><th>Mesai saatleri</th><th>Ekleyen</th><th>Oluşturulma</th><th>IsActive</th><th>IsDeleted</th><th>İşlemler</th></tr></thead><tbody>{pois.map(p=><tr key={p.id}><td>{p.name}</td><td>{p.categoryName}</td><td>{p.parentCategoryName||"-"}</td><td>{p.workingHours||"-"}</td><td>{p.username||"-"}</td><td>{date(p.createdDate)}</td><td>{p.isActive?"Evet":"Hayır"}</td><td>{p.isDeleted?"Evet":"Hayır"}</td><td><button onClick={()=>showDetail(p.id)}>Detay</button>{canUpdatePoi&&!p.isDeleted&&<button onClick={()=>editPoi(p)}>Düzenle</button>}{canDeletePoi&&!p.isDeleted&&<button onClick={()=>deletePoi(p.id)}>Soft Delete</button>}</td></tr>)}</tbody></table>}
      {detail&&<div className="poi-detail-card"><div><h3>{detail.name}</h3><button onClick={()=>setDetail(null)}>Kapat</button></div><p><strong>Kategori:</strong> {detail.parentCategoryName?detail.parentCategoryName+" > ":""}{detail.categoryName}</p><p><strong>Mesai:</strong> {detail.workingHours||"-"}</p><p><strong>Ekleyen:</strong> {detail.username}</p><p><strong>Oluşturulma:</strong> {date(detail.createdDate)}</p></div>}
    </>) : (!canManageCategories ? <Forbidden text="Kategori yönetimi için PoiCategory.Manage yetkisi gerekir."/> : <>
      <form className="admin-form" onSubmit={saveCategory}><input required value={categoryForm.name} onChange={e=>setCategoryForm({...categoryForm,name:e.target.value})} placeholder="Kategori adı"/><input value={categoryForm.description} onChange={e=>setCategoryForm({...categoryForm,description:e.target.value})} placeholder="Açıklama"/><select value={categoryForm.parentId} onChange={e=>setCategoryForm({...categoryForm,parentId:e.target.value})}><option value="">Ana kategori</option>{parents.filter(x=>x.id!==categoryForm.id).map(x=><option key={x.id} value={x.id}>{x.displayName}</option>)}</select><select value={categoryForm.iconKey} onChange={e=>setCategoryForm({...categoryForm,iconKey:e.target.value})}>{iconOptions.map(x=><option key={x.key} value={x.key}>{x.label}</option>)}</select><label className="admin-check"><input type="checkbox" checked={categoryForm.isActive} onChange={e=>setCategoryForm({...categoryForm,isActive:e.target.checked})}/> Aktif</label><button className="primary-button">{categoryForm.id?"Güncelle":"Ekle"}</button>{categoryForm.id&&<button type="button" onClick={()=>setCategoryForm(emptyCategory)}>Vazgeç</button>}</form>
      {loading?<div className="admin-state">Kategoriler yükleniyor…</div>:categories.length===0?<div className="admin-state">Henüz kategori bulunmuyor.</div>:<table className="admin-table"><thead><tr><th>Kategori</th><th>Üst kategori</th><th>İkon</th><th>Açıklama</th><th>Aktif</th><th>Silinmiş</th><th>İşlem</th></tr></thead><tbody>{categories.map(c=><tr key={c.id}><td className={c.parentId?"poi-child-category":"poi-root-category"}>{c.parentId?"↳ ":""}{c.name}</td><td>{c.parentName||"-"}</td><td><img className="poi-icon-preview" src={"/poi-icons/"+(c.iconKey||"default")+".svg"}/></td><td>{c.description||"-"}</td><td>{c.isActive?"Evet":"Hayır"}</td><td>{c.isDeleted?"Evet":"Hayır"}</td><td>{!c.isDeleted&&<><button onClick={()=>setCategoryForm({id:c.id,name:c.name,description:c.description,iconKey:c.iconKey||"default",parentId:c.parentId??"",isActive:c.isActive})}>Düzenle</button><button onClick={()=>deleteCategory(c.id)}>Soft Delete</button></>}</td></tr>)}</tbody></table>}
    </>)}
  </div>;
}

function Forbidden({text}) { return <div className="admin-forbidden"><strong>403</strong><h2>Yetkiniz yok</h2><p>{text}</p></div>; }
