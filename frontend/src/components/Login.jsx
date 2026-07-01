import { useState } from "react";

const LOGIN_URL = "http://localhost:5291/api/Auth/login";

function Login({ onLogin, sessionMessage }) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      const response = await fetch(LOGIN_URL, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({ username, password })
      });

      if (!response.ok) {
        setError("Kullanıcı adı veya şifre hatalı.");
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

  return (
    <main className="login-page">
      <section className="brand-panel">
        <div className="brand-overlay" />
        <div className="brand-content">
          <div className="brand-logo">
            <img src="https://www.basarsoft.com.tr/wp-content/uploads/2024/09/basarsoft-logo.png" alt="Başarsoft" />
          </div>
          <div>
            <span className="brand-kicker">Kurumsal GIS Platformu</span>
            <h1>Harita Yönetim Sistemi</h1>
          </div>
        </div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <div className="login-header">
            <span className="login-kicker">GÜVENLİ GİRİŞ</span>
            <h2>Hesabınıza giriş yapın</h2>
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
              autoComplete="current-password"
            />

            <div className="message-slot">
              {error && <p className="error-message">{error}</p>}
              {!error && sessionMessage && (
                <p className="info-message">{sessionMessage}</p>
              )}
            </div>

            <button type="submit" className="primary-button" disabled={isLoading}>
              {isLoading ? "Giris yapiliyor" : "Giris Yap"}
            </button>
          </form>
        </div>
      </section>
    </main>
  );
}

export default Login;
