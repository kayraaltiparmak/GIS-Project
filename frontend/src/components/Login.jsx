import { useState } from "react";

const LOGIN_URL = "http://localhost:5291/api/Auth/login";
const REGISTER_URL = "http://localhost:5291/api/Auth/register";

function Login({ onLogin, sessionMessage }) {
  const [mode, setMode] = useState("login");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const isRegisterMode = mode === "register";

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");

    const trimmedUsername = username.trim();

    if (!trimmedUsername || !password) {
      setError("Kullanıcı adı ve şifre zorunlu.");
      return;
    }

    if (isRegisterMode && password.length < 6) {
      setError("Şifre en az 6 karakter olmalı.");
      return;
    }

    if (isRegisterMode && password !== confirmPassword) {
      setError("Şifreler eşleşmiyor.");
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch(isRegisterMode ? REGISTER_URL : LOGIN_URL, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ username: trimmedUsername, password })
      });

      if (!response.ok) {
        if (isRegisterMode && response.status === 409) {
          setError("Bu kullanıcı adı zaten kullanılıyor.");
          return;
        }

        setError(isRegisterMode ? "Kayıt oluşturulamadı." : "Kullanıcı adı veya şifre hatalı.");
        return;
      }

      const data = await response.json();

      if (!data.token) {
        setError("Token alınamadı.");
        return;
      }

      onLogin(data.token);
    } catch {
      setError("Sunucuya bağlanılamadı.");
    } finally {
      setIsLoading(false);
    }
  }

  function switchMode(nextMode) {
    setMode(nextMode);
    setError("");
    setPassword("");
    setConfirmPassword("");
  }

  return (
    <main className="login-page">
      <section className="brand-panel">
        <div className="brand-overlay" />
        <div className="brand-content">
          <div className="brand-name">Kayrasoft</div>
          <div>
            <span className="brand-kicker">Kurumsal GIS Platformu</span>
            <h1>Harita Yönetim Sistemi</h1>
          </div>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="login-header">
            <span className="login-kicker">{isRegisterMode ? "YENİ HESAP" : "GÜVENLİ GİRİŞ"}</span>
            <h2>{isRegisterMode ? "Yeni hesap oluşturun" : "Hesabınıza giriş yapın"}</h2>
          </div>

          <div className="auth-switch" aria-label="Oturum modu">
            <button
              type="button"
              className={!isRegisterMode ? "active-auth-mode" : ""}
              onClick={() => switchMode("login")}
            >
              Giriş
            </button>
            <button
              type="button"
              className={isRegisterMode ? "active-auth-mode" : ""}
              onClick={() => switchMode("register")}
            >
              Üye ol
            </button>
          </div>

          <form className="login-form" onSubmit={handleSubmit}>
            <label htmlFor="username">Kullanıcı adı</label>
            <input
              id="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              autoComplete="username"
            />

            <label htmlFor="password">Şifre</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete={isRegisterMode ? "new-password" : "current-password"}
            />

            {isRegisterMode && (
              <>
                <label htmlFor="confirm-password">Şifre tekrar</label>
                <input
                  id="confirm-password"
                  type="password"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                  autoComplete="new-password"
                />
              </>
            )}

            <div className="message-slot">
              {error && <p className="error-message">{error}</p>}
              {!error && sessionMessage && (
                <p className="info-message">{sessionMessage}</p>
              )}
            </div>

            <button type="submit" className="primary-button" disabled={isLoading}>
              {isLoading
                ? (isRegisterMode ? "Kayıt oluşturuluyor" : "Giriş yapılıyor")
                : (isRegisterMode ? "Üye Ol" : "Giriş Yap")}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}

export default Login;
