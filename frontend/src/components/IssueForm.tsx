import { useState, type SubmitEvent } from "react";
import type { CreateIssueInput } from "../api";

type IssueFormProps = {
  busy: boolean;
  priorityLabels: Record<CreateIssueInput["priority"], string>;
  onCreate: (input: CreateIssueInput) => Promise<void>;
};

export function IssueForm({ busy, priorityLabels, onCreate }: IssueFormProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] =
    useState<CreateIssueInput["priority"]>("Medium");
  const [dueDate, setDueDate] = useState("");

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    await onCreate({
      title,
      description,
      priority,
      dueDate: dueDate || null,
    });
  }

  return (
    <form className="editor-form" onSubmit={handleSubmit}>
      <h2>Nouvelle issue</h2>
      <label htmlFor="issue-title">Titre</label>
      <input
        id="issue-title"
        required
        maxLength={150}
        value={title}
        onChange={(event) => setTitle(event.target.value)}
      />
      <label htmlFor="issue-description">Description</label>
      <textarea
        id="issue-description"
        rows={3}
        maxLength={2000}
        value={description}
        onChange={(event) => setDescription(event.target.value)}
      />
      <label htmlFor="issue-priority">Priorité</label>
      <select
        id="issue-priority"
        value={priority}
        onChange={(event) =>
          setPriority(event.target.value as CreateIssueInput["priority"])
        }
      >
        <option value="Low">{priorityLabels.Low}</option>
        <option value="Medium">{priorityLabels.Medium}</option>
        <option value="High">{priorityLabels.High}</option>
        <option value="Critical">{priorityLabels.Critical}</option>
      </select>
      <label htmlFor="issue-due-date">Échéance</label>
      <input
        id="issue-due-date"
        type="date"
        value={dueDate}
        onChange={(event) => setDueDate(event.target.value)}
      />
      <button type="submit" disabled={busy}>
        {busy ? "Création..." : "Créer l'issue"}
      </button>
    </form>
  );
}
