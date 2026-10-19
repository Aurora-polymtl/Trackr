import { useState, type SubmitEvent } from "react";
import type { Issue, IssueFilters } from "../api";

type IssueFiltersFormProps = {
  busy: boolean;
  appliedFilters: IssueFilters;
  priorityLabels: Record<Issue["priority"], string>;
  onApply: (filters: IssueFilters) => Promise<void>;
};

export function IssueFiltersForm({
  busy,
  appliedFilters,
  priorityLabels,
  onApply,
}: IssueFiltersFormProps) {
  const [draft, setDraft] = useState<IssueFilters>(appliedFilters);
  const canClear =
    draft.search !== "" ||
    draft.priority !== "" ||
    appliedFilters.search !== "" ||
    appliedFilters.priority !== "";

  async function handleSubmit(event: SubmitEvent<HTMLElement>) {
    event.preventDefault();
    await onApply(draft);
  }

  async function clearFilters() {
    const empty: IssueFilters = { search: "", priority: "" };
    setDraft(empty);
    await onApply(empty);
  }

  return (
    <form className="issue-filters" onSubmit={handleSubmit}>
      <div className="issue-filter-fields">
        <div>
          <label htmlFor="issue-search">Rechercher une issue</label>
          <input
            id="issue-search"
            type="search"
            value={draft.search}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                search: event.target.value,
              }))
            }
            disabled={busy}
          />
        </div>
        <div>
          <label htmlFor="issue-priority-filter">Filtrer par priorité</label>
          <select
            id="issue-priority-filter"
            value={draft.priority}
            onChange={(event) =>
              setDraft((current) => ({
                ...current,
                priority: event.target.value as IssueFilters["priority"],
              }))
            }
            disabled={busy}
          >
            <option value="">Toutes les priorités</option>
            <option value="Low">{priorityLabels.Low}</option>
            <option value="Medium">{priorityLabels.Medium}</option>
            <option value="High">{priorityLabels.High}</option>
            <option value="Critical">{priorityLabels.Critical}</option>
          </select>
        </div>
      </div>
      <div className="issue-filter-actions">
        <button type="submit" disabled={busy}>
          Appliquer
        </button>
        <button
          className="secondary"
          type="button"
          onClick={clearFilters}
          disabled={busy || !canClear}
        >
          Effacer
        </button>
      </div>
    </form>
  );
}
