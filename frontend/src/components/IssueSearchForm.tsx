import { useState, type SubmitEvent } from "react";

type IssueSearchFormProps = {
  busy: boolean;
  appliedSearch: string;
  onSearch: (search: string) => Promise<void>;
};

export function IssueSearchForm({
  busy,
  appliedSearch,
  onSearch,
}: IssueSearchFormProps) {
  const [draft, setDraft] = useState(appliedSearch);

  async function handleSubmit(event: SubmitEvent<HTMLElement>) {
    event.preventDefault();
    await onSearch(draft.trim());
  }

  async function clearSearch() {
    setDraft("");
    await onSearch("");
  }

  return (
    <form className="issue-search" onSubmit={handleSubmit}>
      <label htmlFor="issue-search">Rechercher une issue</label>
      <div className="issue-search-controls">
        <input
          id="issue-search"
          type="search"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          disabled={busy}
        />
        <button type="submit" disabled={busy}>
          Rechercher
        </button>
        <button
          className="secondary"
          type="button"
          onClick={clearSearch}
          disabled={busy || (!draft && !appliedSearch)}
        >
          Effacer
        </button>
      </div>
    </form>
  );
}
