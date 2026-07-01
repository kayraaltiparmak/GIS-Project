import { useEffect, useState } from "react";
import Login from "./components/Login";
import Map from "./components/Map";
import "./App.css";

const TOKEN_KEY = "token";
const SESSION_EXPIRES_AT_KEY = "sessionExpiresAt";
const SESSION_DURATION_MS = 5 * 60 * 1000;

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
  const [remainingSeconds, setRemainingSeconds] = useState(0);
  const [sessionMessage, setSessionMessage] = useState("");

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
        localStorage.removeItem(SESSION_EXPIRES_AT_KEY);
        setToken(null);
        setExpiresAt(null);
        setSessionMessage("Oturum suresi doldu, tekrar giris yapin.");
      }
    }

    updateTimer();
    const timerId = window.setInterval(updateTimer, 1000);

    return () => window.clearInterval(timerId);
  }, [token, expiresAt]);

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
    localStorage.removeItem(SESSION_EXPIRES_AT_KEY);
    setToken(null);
    setExpiresAt(null);
    setSessionMessage("");
  }

  return token ? (
    <Map remainingSeconds={remainingSeconds} onLogout={handleLogout} />
  ) : (
    <Login onLogin={handleLogin} sessionMessage={sessionMessage} />
  );
}

export default App;
