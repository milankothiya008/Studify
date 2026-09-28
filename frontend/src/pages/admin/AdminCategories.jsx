import { useEffect, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import api, { getErrorMessage } from "../../api";
import { useToast } from "../../ToastContext";
import { SkeletonRows } from "../../components/Skeleton";

function AdminCategories() {
  const showToast = useToast();
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [newName, setNewName] = useState("");

  function loadCategories() {
    api
      .get("/categories")
      .then(function (response) {
        setCategories(response.data);
      })
      .finally(function () {
        setLoading(false);
      });
  }

  useEffect(function () {
    loadCategories();
  }, []);

  async function handleAdd(event) {
    event.preventDefault();
    try {
      await api.post("/categories", { name: newName });
      setNewName("");
      showToast("Category added.");
      loadCategories();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function handleRename(category) {
    const name = window.prompt("New name for the category:", category.name);
    if (!name) {
      return;
    }
    try {
      await api.put("/categories/" + category.id, { name: name });
      showToast("Category renamed.");
      loadCategories();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  async function handleDelete(category) {
    if (!window.confirm('Delete category "' + category.name + '"? Its courses will have no category.')) {
      return;
    }
    try {
      await api.delete("/categories/" + category.id);
      showToast("Category deleted.");
      loadCategories();
    } catch (err) {
      showToast(getErrorMessage(err), "error");
    }
  }

  return (
    <div>
      <form className="add-row table-toolbar" onSubmit={handleAdd}>
        <input
          placeholder="New category name"
          value={newName}
          maxLength={100}
          onChange={(e) => setNewName(e.target.value)}
          required
        />
        <button type="submit" className="btn btn-primary">
          <Plus size={18} /> Add category
        </button>
      </form>

      {loading ? (
        <SkeletonRows rows={5} />
      ) : (
        <div className="card table-card">
          <div className="table-wrapper">
            <table className="table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Published courses</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {categories.map(function (category) {
                  return (
                    <tr key={category.id}>
                      <td>
                        <strong>{category.name}</strong>
                      </td>
                      <td>{category.courseCount}</td>
                      <td>
                        <div className="table-actions">
                          <button className="icon-button" title="Rename" onClick={() => handleRename(category)}>
                            <Pencil size={16} />
                          </button>
                          <button className="icon-button danger" title="Delete" onClick={() => handleDelete(category)}>
                            <Trash2 size={16} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminCategories;
