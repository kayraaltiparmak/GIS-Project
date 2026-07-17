import { useEffect, useState } from "react";
import AdminPanel from "./components/AdminPanel";
import Login from "./components/Login";
import Map from "./components/Map";
import "./App.css";

const TOKEN_KEY = "token";
const USER_INFO_KEY = "userInfo";
const SESSION_EXPIRES_AT_KEY = "sessionExpiresAt";
const SESSION_DURATION_MS = 10 * 60 * 1000;
const ME_URL = "http://localhost:5291/api/Auth/me";

function getInitialSession() {
  const token = localStorage.getItem(TOKEN_KEY);
  const expiresAt = Number(localStorage.getItem(SESSION_EXPIRES_AT_KEY));

  if (!token) {
    return { token: null, expiresAt: null };
  }

  if (expiresAt && expiresAt <= Date.now()) {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(SESSION_EXPIRES_AT_KEY);
    return { token: null, expiresAt: null };
  }

  return {
    token,
    expiresAt: expiresAt || Date.now() + SESSION_DURATION_MS
  };
}

function App() {
  const initialSession = getInitialSession();
  const [token, setToken] = useState(initialSession.token);
  const [expiresAt, setExpiresAt] = useState(initialSession.expiresAt);
  const [userInfo, setUserInfo] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem(USER_INFO_KEY)) ?? null;
    } catch {
      return null;
    }
  });
  const [isUserLoading, setIsUserLoading] = useState(Boolean(initialSession.token));
  const [remainingSeconds, setRemainingSeconds] = useState(0);
  const [sessionMessage, setSessionMessage] = useState("");
  const [currentPath, setCurrentPath] = useState(window.location.pathname);

  useEffect(() => {
    function handlePopState() {
      setCurrentPath(window.location.pathname);
    }

    window.addEventListener("popstate", handlePopState);

    return () => window.removeEventListener("popstate", handlePopState);
  }, []);

  useEffect(() => {
    if (!token || !expiresAt) {
      setRemainingSeconds(0);
      return;
    }

    localStorage.setItem(SESSION_EXPIRES_AT_KEY, String(expiresAt));

    function updateTimer() {
      const nextRemainingSeconds = Math.max(
        0,
        Math.ceil((expiresAt - Date.now()) / 1000)
      );

      setRemainingSeconds(nextRemainingSeconds);

      if (nextRemainingSeconds === 0) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_INFO_KEY);
        localStorage.removeItem(SESSION_EXPIRES_AT_KEY);
        setToken(null);
        setExpiresAt(null);
        setUserInfo(null);
        setSessionMessage("Oturum suresi doldu, tekrar giris yapin.");
      }
    }

    updateTimer();
    const timerId = window.setInterval(updateTimer, 1000);

    return () => window.clearInterval(timerId);
  }, [token, expiresAt]);

  useEffect(() => {
    if (!token) {
      setUserInfo(null);
      setIsUserLoading(false);
      return;
    }

    let isCancelled = false;

    async function loadCurrentUser() {
      setIsUserLoading(true);

      try {
        const response = await fetch(ME_URL, {
          headers: {
            "Authorization": "Bearer " + token
          }
        });

        if (response.status === 401) {
          handleLogout();
          return;
        }

        if (!response.ok) {
          setSessionMessage("Kullanici bilgileri alinamadi, tekrar giris yapin.");
          handleLogout();
          return;
        }

        const data = await response.json();

        if (!isCancelled) {
          localStorage.setItem(USER_INFO_KEY, JSON.stringify(data));
          setUserInfo(data);
        }
      } catch {
        if (!isCancelled) {
          setSessionMessage("Sunucuya baglanilamadi, tekrar giris yapin.");
          handleLogout();
        }
      } finally {
        if (!isCancelled) {
          setIsUserLoading(false);
        }
      }
    }

    loadCurrentUser();

    return () => {
      isCancelled = true;
    };
  }, [token]);

  function handleLogin(nextToken) {
    const nextExpiresAt = Date.now() + SESSION_DURATION_MS;

    localStorage.setItem(TOKEN_KEY, nextToken);
    localStorage.setItem(SESSION_EXPIRES_AT_KEY, String(nextExpiresAt));
    setToken(nextToken);
    setExpiresAt(nextExpiresAt);
    setSessionMessage("");
  }

  function handleLogout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_INFO_KEY);
    localStorage.removeItem(SESSION_EXPIRES_AT_KEY);
    setToken(null);
    setExpiresAt(null);
    setUserInfo(null);
    setSessionMessage("");
    navigateTo("/");
  }

  function navigateTo(path) {
    if (window.location.pathname !== path) {
      window.history.pushState({}, "", path);
    }

    setCurrentPath(path);
  }

  function canOpenAdminPanel() {
    const permissions = userInfo?.permissions ?? [];

    return Boolean(userInfo?.isAdmin) ||
      permissions.includes("User.Manage") ||
      permissions.includes("Role.Manage") ||
      permissions.includes("Permission.Manage") ||
      permissions.includes("Poi.Manage") ||
      permissions.includes("PoiCategory.Manage");
  }

  if (token && isUserLoading && !userInfo) {
    return (
      <main className="app-loading">
        <div>Yükleniyor</div>
      </main>
    );
  }

  if (!token) {
    return <Login onLogin={handleLogin} sessionMessage={sessionMessage} />;
  }

  return currentPath.startsWith("/admin") && (canOpenAdminPanel() || currentPath === "/admin/pois") ? (
    <AdminPanel
      currentPath={currentPath}
      token={token}
      userInfo={userInfo}
      onBackToMap={() => navigateTo("/")}
      onNavigate={navigateTo}
    />
  ) : (
    <Map
      remainingSeconds={remainingSeconds}
      userInfo={userInfo}
      onAdminOpen={() => navigateTo("/admin")}
      onLogout={handleLogout}
    />
  );
}

export default App;
