import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { useState } from "react";
import { SelectablePaginatedTable } from "./SelectablePaginatedTable";
import { usePaginatedSelection } from "@/hooks/usePaginatedSelection";

afterEach(cleanup);

it("sorts by headers by default, comparing raw numbers and preserving selection", () => {
  render(<SelectablePaginatedTable rows={[{ id: "b", amount: 100 }, { id: "a", amount: 9 }]}
    columns={[{ id: "amount", header: "Valor", render: (row) => `R$ ${row.amount}` },
      { id: "actions", header: "Ações", sortable: false, render: () => "Editar" }]}
    getRowId={(row) => row.id} selectedRows={[{ id: "b", amount: 100 }]} onSelectionChange={() => {}}
    pagination={{ page: 1, pageSize: 20, totalItems: 2, onPageChange: () => {} }} />);
  fireEvent.click(screen.getByText("Valor"));
  expect(within(screen.getAllByRole("row")[1]).getByText("R$ 9")).toBeVisible();
  fireEvent.click(screen.getByText("Valor"));
  expect(within(screen.getAllByRole("row")[1]).getByText("R$ 100")).toBeVisible();
  expect(screen.getByRole("checkbox", { name: "Selecionar registro b" })).toBeChecked();
  expect(within(screen.getByRole("columnheader", { name: "Ações" })).queryByRole("button")).toBeNull();
});

it("sorts the complete local dataset before paging", () => {
  function LocalTable() {
    const [page, setPage] = useState(2);
    return <SelectablePaginatedTable rows={[{ id: 1, name: "Zeca" }, { id: 2, name: "Ana" }, { id: 3, name: "Bia" }]}
      paginationMode="client" columns={[{ id: "name", header: "Nome", render: (row) => row.name }]}
      getRowId={(row) => row.id} selectedRows={[]} onSelectionChange={() => {}} showSelection={false}
      pagination={{ page, pageSize: 2, totalItems: 3, onPageChange: setPage }} />;
  }
  render(<LocalTable />);
  fireEvent.click(screen.getByText("Nome"));
  expect(screen.getByText("Ana")).toBeVisible();
  expect(screen.queryByText("Zeca")).toBeNull();
});

type Contact = { code: number; name: string; available: boolean };
const contacts: Contact[] = [
  { code: 11, name: "Ana", available: true },
  { code: 12, name: "Bia", available: false },
  { code: 21, name: "Caio", available: true },
];

function ContactsTable({ scope = "company-a" }: { scope?: string }) {
  const [page, setPage] = useState(1);
  const [order, setOrder] = useState<"asc" | "desc">("asc");
  const selection = usePaginatedSelection<Contact>({ scopeKey: scope });
  return (
    <SelectablePaginatedTable
      rows={page === 1 ? contacts.slice(0, 2) : contacts.slice(2)}
      columns={[{ id: "name", header: "Nome", sortKey: "name", render: (row) => row.name }]}
      getRowId={(row) => row.code}
      isRowSelectable={(row) => row.available}
      getRowSelectionLabel={(row) => `Selecionar ${row.name}`}
      selectedRows={selection.selectedRows}
      onSelectionChange={selection.setSelectedRows}
      pagination={{ page, pageSize: 2, totalItems: 3, onPageChange: setPage }}
      sortBy="name"
      sortDirection={order}
      onSort={() => setOrder(order === "asc" ? "desc" : "asc")}
      selectionActions={
        <div>
          <output aria-label="Contatos selecionados">
            {selection.selectedRows.map((row) => row.name).join(", ")}
          </output>
          <button onClick={selection.clearSelection}>Limpar seleção</button>
        </div>
      }
    />
  );
}

it("preserves selected records on other pages and selects only eligible rows on the current page", () => {
  render(<ContactsTable />);
  fireEvent.click(
    screen.getByRole("checkbox", { name: "Selecionar todos os registros desta página" }),
  );
  expect(screen.getByRole("checkbox", { name: "Selecionar Bia" })).toBeDisabled();
  expect(screen.getByLabelText("Contatos selecionados")).toHaveTextContent(/^Ana$/);
  fireEvent.click(screen.getByRole("button", { name: /page 2/i }));
  fireEvent.click(screen.getByRole("checkbox", { name: "Selecionar Caio" }));
  expect(screen.getByLabelText("Contatos selecionados")).toHaveTextContent("Ana, Caio");
  fireEvent.click(
    screen.getByRole("checkbox", { name: "Selecionar todos os registros desta página" }),
  );
  expect(screen.getByLabelText("Contatos selecionados")).toHaveTextContent(/^Ana$/);
  fireEvent.click(screen.getByRole("button", { name: /page 1/i }));
  expect(screen.getByRole("checkbox", { name: "Selecionar Ana" })).toBeChecked();
});

it("keeps selection when sorting and lets the caller clear it explicitly", () => {
  render(<ContactsTable />);
  fireEvent.click(screen.getByRole("checkbox", { name: "Selecionar Ana" }));
  fireEvent.click(screen.getByText("Nome"));
  expect(screen.getByRole("columnheader", { name: /Nome/ })).toHaveAttribute(
    "aria-sort",
    "descending",
  );
  expect(screen.getByLabelText("Contatos selecionados")).toHaveTextContent("Ana");
  fireEvent.click(screen.getByRole("button", { name: "Limpar seleção" }));
  expect(screen.getByRole("checkbox", { name: "Selecionar Ana" })).not.toBeChecked();
});

it("clears selection immediately on a context change and does not resurrect it when returning", () => {
  const view = render(<ContactsTable />);
  fireEvent.click(screen.getByRole("checkbox", { name: "Selecionar Ana" }));
  view.rerender(<ContactsTable scope="company-b" />);
  expect(screen.getByLabelText("Contatos selecionados")).toBeEmptyDOMElement();
  view.rerender(<ContactsTable scope="company-a" />);
  expect(screen.getByLabelText("Contatos selecionados")).toBeEmptyDOMElement();
});

it("disables selection, sorting and paging while a page is loading", () => {
  render(
    <SelectablePaginatedTable
      rows={contacts}
      columns={[{ id: "name", header: "Nome", sortKey: "name", render: (row) => row.name }]}
      getRowId={(row) => row.code}
      selectedRows={[]}
      onSelectionChange={() => {}}
      loading
      onSort={() => {}}
      pagination={{ page: 1, pageSize: 2, totalItems: 3, onPageChange: () => {} }}
    />,
  );
  expect(screen.getAllByRole("checkbox").every((input) => input.hasAttribute("disabled"))).toBe(
    true,
  );
  expect(screen.getByRole("button", { name: /page 2/i })).toBeDisabled();
});
