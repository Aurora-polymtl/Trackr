import { useState, type SubmitEvent } from "react";
import {
  getProjects,
  signIn,
  signUp,
  type Project,
  type Session,
} from "../api";

type AuthScreenProps = {
  onAuthenticated: (session: Session, projects: Project[]) => void;
};

export function AuthScreen({ onAuthenticated }: AuthScreenProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authMode, setAuthMode] = useState<"login" | "register">("login");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function handleAuth(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setNotice("");

    if (authMode === "register" && password !== confirmPassword) {
      setError("Les mots de passe ne correspondent pas.");
      return;
    }

    setBusy(true);

    try {
      if (authMode === "register") {
        await signUp(email.trim(), password);
        setPassword("");
        setConfirmPassword("");
        setAuthMode("login");
        setNotice("Inscription réussie. Veuillez vous connecter.");
        return;
      }
      const nextSession = await signIn(email.trim(), password);
      const nextProjects = await getProjects(nextSession.accessToken);
      onAuthenticated(nextSession, nextProjects);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Erreur inattendue.");
    } finally {
      setBusy(false);
    }
  }

  function switchAuthMode() {
    setAuthMode(authMode === "login" ? "register" : "login");
    setPassword("");
    setConfirmPassword("");
    setError("");
    setNotice("");
  }

  return (
    <main className="login-page">
      <form className="login-form" onSubmit={handleAuth}>
        <strong className="brand">Trackr</strong>
        <h1>{authMode === "login" ? "Connexion" : "Créer un compte"}</h1>
        <label htmlFor="email">Adresse courriel</label>
        <input
          id="email"
          type="email"
          autoComplete="username"
          required
          value={email}
          onChange={(event) => setEmail(event.target.value)}
        />
        <label htmlFor="password">Mot de passe</label>
        <input
          id="password"
          type="password"
          autoComplete={
            authMode === "register" ? "new-password" : "current-password"
          }
          required
          value={password}
          onChange={(event) => setPassword(event.target.value)}
        />
        {authMode === "register" && (
          <>
            <p className="password-hint">
              Au moins 6 caractères, avec majuscule, minuscule, chiffre et
              symbole.
            </p>
            <label htmlFor="confirm-password">Confirmer le mot de passe</label>
            <input
              id="confirm-password"
              type="password"
              autoComplete="new-password"
              required
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
            />
          </>
        )}
        {notice && (
          <p className="notice" role="status">
            {notice}
          </p>
        )}
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
        <button type="submit" disabled={busy}>
          {busy
            ? "Veuillez patienter..."
            : authMode === "login"
              ? "Se connecter"
              : "Créer mon compte"}
        </button>
        <button
          className="auth-toggle"
          type="button"
          onClick={switchAuthMode}
          disabled={busy}
        >
          {authMode === "login"
            ? "Créer un compte"
            : "Déjà inscrit? Se connecter"}
        </button>
      </form>
    </main>
  );
}
