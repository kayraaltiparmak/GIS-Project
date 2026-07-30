import { useEffect, useMemo, useState } from "react";
import GeographicPermissionPanel from "./GeographicPermissionPanel";
import PoiAdminPanel from "./PoiAdminPanel";

const API_URL = "http://localhost:5291/api/Admin";

const adminSections = [
  { path: "/admin", label: "Dashboard" },
  { path: "/admin/users", label: "Kullanıcılar" },
  { path: "/admin/roles", label: "Roller" },
  { path: "/admin/permissions", label: "Yetkiler" },
  { path: "/admin/role-permissions", label: "Role Yetki Ata" },
  { path: "/admin/user-permissions", label: "Kullanıcı Yetkileri" },
  { path: "/admin/geo-permissions", label: "Coğrafi Yetki" },
  { path: "/admin/pois", label: "POI Yönetimi" }
];

const emptyUserForm = { id: null, username: "", password: "", roleId: "", isActive: true };
const emptyRoleForm = { id: null, name: "", description: "", isActive: true };
const emptyPermissionForm = { id: null, name: "", code: "", description: "", isActive: true };

function AdminPanel({ currentPath, token, userInfo, onBackToMap, onNavigate }) {
  const permissionSet = useMemo(() => new Set(userInfo?.permissions ?? []), [userInfo]);
  const canAccessPois = permissionSet.has("Poi.Manage") || permissionSet.has("PoiCategory.Manage");
  const visibleSections = useMemo(
    () => adminSections.filter((section) => section.path !== "/admin/pois" || canAccessPois),
    [canAccessPois]
  );
  const [users, setUsers] = useState([]);
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState([]);
  const [rolePermissions, setRolePermissions] = useState([]);
  const [userPermissions, setUserPermissions] = useState([]);
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [selectedUserId, setSelectedUserId] = useState("");
  const [userForm, setUserForm] = useState(emptyUserForm);
  const [roleForm, setRoleForm] = useState(emptyRoleForm);
  const [permissionForm, setPermissionForm] = useState(emptyPermissionForm);
  const [message, setMessage] = useState("");

  const activeSection = useMemo(
    () => adminSections.find((section) => section.path === currentPath) ?? adminSections[0],
    [currentPath]
  );

  async function apiRequest(path, options = {}) {
    const response = await fetch(API_URL + path, {
      ...options,
      headers: {
        "Authorization": "Bearer " + token,
        "Content-Type": "application/json",
        ...(options.headers ?? {})
      }
    });

    if (response.status === 403) {
      throw new Error("Bu işlem için yetkiniz yok.");
    }

    if (!response.ok) {
      throw new Error("İşlem tamamlanamadı.");
    }

    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  async function loadUsers() {
    setUsers(await apiRequest("/users"));
  }

  async function loadRoles() {
    setRoles(await apiRequest("/roles"));
  }

  async function loadRoleOptions() {
    setRoles(await apiRequest("/role-options"));
  }

  async function loadPermissions() {
    setPermissions(await apiRequest("/permissions"));
  }

  async function loadUserOptions() {
    setUsers(await apiRequest("/user-options"));
  }

  async function loadBaseData() {
    setMessage("");

    try {
      const loaders = [];

      if (userInfo?.permissions?.includes("User.Manage")) {
        loaders.push(loadUsers());
      } else if (userInfo?.permissions?.includes("Permission.Manage")) {
        loaders.push(loadUserOptions());
      }

      if (userInfo?.permissions?.includes("Role.Manage")) {
        loaders.push(loadRoles());
      } else if (userInfo?.permissions?.includes("Permission.Manage")) {
        loaders.push(loadRoleOptions());
      }

      if (userInfo?.permissions?.includes("Permission.Manage")) {
        loaders.push(loadPermissions());
      }

      await Promise.all(loaders);
    } catch (error) {
      setMessage(error.message);
    }
  }

  useEffect(() => {
    loadBaseData();
  }, [token]);

  useEffect(() => {
    if (!selectedRoleId) {
      setRolePermissions([]);
      return;
    }

    apiRequest("/role-permissions/" + selectedRoleId)
      .then(setRolePermissions)
      .catch((error) => setMessage(error.message));
  }, [selectedRoleId]);

  useEffect(() => {
    if (!selectedUserId) {
      setUserPermissions([]);
      return;
    }

    apiRequest("/user-permissions/" + selectedUserId)
      .then(setUserPermissions)
      .catch((error) => setMessage(error.message));
  }, [selectedUserId]);

  async function submitUser(event) {
    event.preventDefault();
    setMessage("");

    try {
      const payload = {
        username: userForm.username,
        password: userForm.password || null,
        roleId: userForm.roleId ? Number(userForm.roleId) : null,
        isActive: userForm.isActive
      };

      if (userForm.id) {
        await apiRequest("/users/" + userForm.id, { method: "PUT", body: JSON.stringify(payload) });
      } else {
        await apiRequest("/users", { method: "POST", body: JSON.stringify(payload) });
      }

      setUserForm(emptyUserForm);
      await loadUsers();
      setMessage("Kullanıcı kaydedildi.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function submitRole(event) {
    event.preventDefault();
    setMessage("");

    try {
      const payload = {
        name: roleForm.name,
        description: roleForm.description,
        isActive: roleForm.isActive
      };

      if (roleForm.id) {
        await apiRequest("/roles/" + roleForm.id, { method: "PUT", body: JSON.stringify(payload) });
      } else {
        await apiRequest("/roles", { method: "POST", body: JSON.stringify(payload) });
      }

      setRoleForm(emptyRoleForm);
      await loadRoles();
      setMessage("Rol kaydedildi.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function submitPermission(event) {
    event.preventDefault();
    setMessage("");

    try {
      const payload = {
        name: permissionForm.name,
        code: permissionForm.code,
        description: permissionForm.description,
        isActive: permissionForm.isActive
      };

      if (permissionForm.id) {
        await apiRequest("/permissions/" + permissionForm.id, { method: "PUT", body: JSON.stringify(payload) });
      } else {
        await apiRequest("/permissions", { method: "POST", body: JSON.stringify(payload) });
      }

      setPermissionForm(emptyPermissionForm);
      await loadPermissions();
      setMessage("Yetki kaydedildi.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function softDelete(type, id) {
    setMessage("");

    try {
      await apiRequest("/" + type + "/" + id, { method: "DELETE" });

      if (type === "users") {
        await loadUsers();
      } else if (type === "roles") {
        await loadRoles();
      } else {
        await loadPermissions();
      }

      setMessage("Kayıt pasife alındı.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  function toggleRolePermission(permissionId) {
    setRolePermissions((current) =>
      current.map((permission) =>
        permission.id === permissionId ? { ...permission, assigned: !permission.assigned } : permission
      )
    );
  }

  async function saveRolePermissions() {
    setMessage("");

    try {
      await apiRequest("/role-permissions/" + selectedRoleId, {
        method: "PUT",
        body: JSON.stringify({
          permissionIds: rolePermissions.filter((permission) => permission.assigned).map((permission) => permission.id)
        })
      });
      setMessage("Rol yetkileri güncellendi.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  function toggleUserPermission(permissionId) {
    setUserPermissions((current) =>
      current.map((permission) =>
        permission.id === permissionId && !permission.fromRole
          ? { ...permission, extra: !permission.extra }
          : permission
      )
    );
  }

  async function saveUserPermissions() {
    setMessage("");

    try {
      await apiRequest("/user-permissions/" + selectedUserId, {
        method: "PUT",
        body: JSON.stringify({
          permissionIds: userPermissions.filter((permission) => permission.extra && !permission.fromRole).map((permission) => permission.id)
        })
      });
      setMessage("Kullanıcı yetkileri güncellendi.");
    } catch (error) {
      setMessage(error.message);
    }
  }

  function renderUsers() {
    return (
      <AdminCrudLayout
        title="Kullanıcılar"
        form={(
          <form className="admin-form" onSubmit={submitUser}>
            <input value={userForm.username} onChange={(event) => setUserForm({ ...userForm, username: event.target.value })} placeholder="Kullanıcı adı" />
            <input type="password" value={userForm.password} onChange={(event) => setUserForm({ ...userForm, password: event.target.value })} placeholder={userForm.id ? "Yeni şifre (opsiyonel)" : "Şifre"} />
            <select value={userForm.roleId} onChange={(event) => setUserForm({ ...userForm, roleId: event.target.value })}>
              <option value="">Rol yok</option>
              {roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}
            </select>
            <label className="admin-check"><input type="checkbox" checked={userForm.isActive} onChange={(event) => setUserForm({ ...userForm, isActive: event.target.checked })} /> Aktif</label>
            <button type="submit" className="primary-button">{userForm.id ? "Güncelle" : "Ekle"}</button>
            {userForm.id && <button type="button" className="secondary-button" onClick={() => setUserForm(emptyUserForm)}>Vazgeç</button>}
          </form>
        )}
      >
        <table className="admin-table">
          <thead><tr><th>ID</th><th>Kullanıcı</th><th>Rol</th><th>Durum</th><th>İşlem</th></tr></thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>{user.id}</td>
                <td>{user.username}</td>
                <td>{user.roleName || "-"}</td>
                <td>{user.isActive ? "Aktif" : "Pasif"}</td>
                <td>
                  <button type="button" onClick={() => setUserForm({ id: user.id, username: user.username, password: "", roleId: user.roleId ?? "", isActive: user.isActive })}>Düzenle</button>
                  <button type="button" onClick={() => softDelete("users", user.id)}>Sil</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </AdminCrudLayout>
    );
  }

  function renderRoles() {
    return (
      <AdminCrudLayout title="Roller" form={<RoleForm form={roleForm} setForm={setRoleForm} onSubmit={submitRole} />}>
        <SimpleTable
          records={roles}
          columns={["id", "name", "description", "isActive"]}
          onEdit={(role) => setRoleForm({ id: role.id, name: role.name, description: role.description, isActive: role.isActive })}
          onDelete={(role) => softDelete("roles", role.id)}
        />
      </AdminCrudLayout>
    );
  }

  function renderPermissions() {
    return (
      <AdminCrudLayout title="Yetkiler" form={<PermissionForm form={permissionForm} setForm={setPermissionForm} onSubmit={submitPermission} />}>
        <SimpleTable
          records={permissions}
          columns={["id", "code", "name", "description", "isActive"]}
          onEdit={(permission) => setPermissionForm({ id: permission.id, name: permission.name, code: permission.code, description: permission.description, isActive: permission.isActive })}
          onDelete={(permission) => softDelete("permissions", permission.id)}
        />
      </AdminCrudLayout>
    );
  }

  function renderRolePermissions() {
    return (
      <AdminCrudLayout title="Role Yetki Ata">
        <div className="admin-assignment-bar">
          <select value={selectedRoleId} onChange={(event) => setSelectedRoleId(event.target.value)}>
            <option value="">Rol seç</option>
            {roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}
          </select>
          <button type="button" className="secondary-button" disabled={!selectedRoleId} onClick={() => setRolePermissions((current) => current.map((permission) => ({ ...permission, assigned: true })))}>
            Tümünü Seç
          </button>
          <button type="button" className="primary-button" disabled={!selectedRoleId} onClick={saveRolePermissions}>
            Kaydet
          </button>
        </div>
        <PermissionChecklist permissions={rolePermissions} checkedKey="assigned" onToggle={toggleRolePermission} />
      </AdminCrudLayout>
    );
  }

  function renderUserPermissions() {
    return (
      <AdminCrudLayout title="Kullanıcı Yetkileri">
        <div className="admin-assignment-bar">
          <select value={selectedUserId} onChange={(event) => setSelectedUserId(event.target.value)}>
            <option value="">Kullanıcı seç</option>
            {users.map((user) => <option key={user.id} value={user.id}>{user.username}</option>)}
          </select>
          <button type="button" className="primary-button" disabled={!selectedUserId} onClick={saveUserPermissions}>
            Kaydet
          </button>
        </div>
        <PermissionChecklist permissions={userPermissions} checkedKey="extra" disabledKey="fromRole" onToggle={toggleUserPermission} />
      </AdminCrudLayout>
    );
  }

  function renderContent() {
    if (activeSection.path === "/admin/users") return renderUsers();
    if (activeSection.path === "/admin/roles") return renderRoles();
    if (activeSection.path === "/admin/permissions") return renderPermissions();
    if (activeSection.path === "/admin/role-permissions") return renderRolePermissions();
    if (activeSection.path === "/admin/user-permissions") return renderUserPermissions();
    if (activeSection.path === "/admin/pois") return canAccessPois
      ? <PoiAdminPanel token={token} userInfo={userInfo} />
      : <div className="admin-forbidden"><strong>403</strong><h2>Yetkiniz yok</h2><p>POI Yönetimi için Poi.Manage veya PoiCategory.Manage yetkisi gerekir.</p></div>;
    if (activeSection.path === "/admin/geo-permissions") {
      return <GeographicPermissionPanel token={token} userInfo={userInfo} />;
    }

    return (
      <div className="admin-placeholder">
        <span>{userInfo?.username}</span>
        <h2>{activeSection.label}</h2>
        <p>Bu bölüm admin panelinin sonraki aşaması için hazırlandı.</p>
      </div>
    );
  }

  return (
    <main className="admin-page">
      <header className="admin-navbar">
        <div className="admin-navbar-title">
          <span className="topbar-kicker">Admin Paneli</span>
          <h1>{activeSection.label}</h1>
        </div>

        <nav className="admin-navbar-actions" aria-label="Admin menü">
          {visibleSections.map((section) => (
            <button
              type="button"
              key={section.path}
              className={activeSection.path === section.path ? "active-admin-tab" : ""}
              onClick={() => onNavigate(section.path)}
            >
              {section.label}
            </button>
          ))}
          <button type="button" className="admin-map-button" onClick={onBackToMap}>
            Haritaya Dön
          </button>
        </nav>
      </header>

      <section className="admin-content">
        {message && <div className="admin-message">{message}</div>}
        {renderContent()}
      </section>
    </main>
  );
}

function AdminCrudLayout({ title, form, children }) {
  return (
    <div className="admin-crud">
      <div className="admin-crud-header">
        <h2>{title}</h2>
      </div>
      {form}
      {children}
    </div>
  );
}

function RoleForm({ form, setForm, onSubmit }) {
  return (
    <form className="admin-form role-form" onSubmit={onSubmit}>
      <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Rol adı" />
      <input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Açıklama" />
      <label className="admin-check"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} /> Aktif</label>
      <button type="submit" className="primary-button">{form.id ? "Güncelle" : "Ekle"}</button>
    </form>
  );
}

function PermissionForm({ form, setForm, onSubmit }) {
  return (
    <form className="admin-form" onSubmit={onSubmit}>
      <input value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} placeholder="Kod" />
      <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="Yetki adı" />
      <input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="Açıklama" />
      <label className="admin-check"><input type="checkbox" checked={form.isActive} onChange={(event) => setForm({ ...form, isActive: event.target.checked })} /> Aktif</label>
      <button type="submit" className="primary-button">{form.id ? "Güncelle" : "Ekle"}</button>
    </form>
  );
}

function SimpleTable({ records, columns, onEdit, onDelete }) {
  return (
    <table className="admin-table">
      <thead>
        <tr>{columns.map((column) => <th key={column}>{column}</th>)}<th>İşlem</th></tr>
      </thead>
      <tbody>
        {records.map((record) => (
          <tr key={record.id}>
            {columns.map((column) => <td key={column}>{typeof record[column] === "boolean" ? (record[column] ? "Aktif" : "Pasif") : record[column]}</td>)}
            <td>
              <button type="button" onClick={() => onEdit(record)}>Düzenle</button>
              <button type="button" onClick={() => onDelete(record)}>Sil</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function PermissionChecklist({ permissions, checkedKey, disabledKey, onToggle }) {
  return (
    <div className="permission-checklist">
      {permissions.map((permission) => (
        <label key={permission.id} className={permission[disabledKey] ? "permission-check readonly" : "permission-check"}>
          <input
            type="checkbox"
            checked={Boolean(permission[checkedKey] || permission[disabledKey])}
            disabled={Boolean(permission[disabledKey])}
            onChange={() => onToggle(permission.id)}
          />
          <span>{permission.code}</span>
          <strong>{permission.source || permission.name}</strong>
        </label>
      ))}
    </div>
  );
}

export default AdminPanel;
