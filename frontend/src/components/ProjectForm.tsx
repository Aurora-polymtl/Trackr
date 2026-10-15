import { useState, type SubmitEvent } from "react";

type ProjectFormProps = {
  busy: boolean;
  onCreate: (name: string, description: string) => Promise<void>;
};

export function ProjectForm({ busy, onCreate }: ProjectFormProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate(name, description);
  }

  return (
    <form className="editor-form" onSubmit={handleSubmit}>
      <h2>Nouveau projet</h2>
      <label htmlFor="project-name">Nom du projet</label>
      <input
        id="project-name"
        required
        maxLength={100}
        value={name}
        onChange={(event) => setName(event.target.value)}
      />
      <label htmlFor="project-description">Description</label>
      <textarea
        id="project-description"
        rows={3}
        maxLength={500}
        value={description}
        onChange={(event) => setDescription(event.target.value)}
      />
      <button type="submit" disabled={busy}>
        {busy ? "Création..." : "Créer le projet"}
      </button>
    </form>
  );
}
