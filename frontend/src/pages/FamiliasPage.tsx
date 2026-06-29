import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { Familia } from '../types';
import { formatUYU } from '../types';
import { validateFamilia, LIMITS } from '../utils/validation';
import { Plus, Edit2, Trash2 } from 'lucide-react';

const emptyForm = { nombre: '', cuotaMensual: 3500, observaciones: '' };

export default function FamiliasPage() {
  const [familias, setFamilias] = useState<Familia[]>([]);
  const [buscar, setBuscar] = useState('');
  const [modal, setModal] = useState(false);
  const [editId, setEditId] = useState<number | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [errors, setErrors] = useState<string[]>([]);
  const [confirmDelete, setConfirmDelete] = useState<Familia | null>(null);
  const [buscarDebounced, setBuscarDebounced] = useState('');

  useEffect(() => {
    const t = setTimeout(() => setBuscarDebounced(buscar), 300);
    return () => clearTimeout(t);
  }, [buscar]);

  const load = () => api.familias.list(buscarDebounced || undefined).then(setFamilias).catch(console.error);
  useEffect(() => { load(); }, [buscarDebounced]);

  const openNew = () => { setEditId(null); setForm(emptyForm); setErrors([]); setModal(true); };

  const openEdit = (f: Familia) => {
    setEditId(f.id);
    setForm({ nombre: f.nombre, cuotaMensual: f.cuotaMensual, observaciones: f.observaciones || '' });
    setErrors([]);
    setModal(true);
  };

  const save = async () => {
    setErrors([]);
    const validationErrors = validateFamilia(form);
    if (validationErrors.length > 0) { setErrors(validationErrors); return; }
    try {
      const payload = {
        nombre: form.nombre.trim(),
        cuotaMensual: form.cuotaMensual,
        observaciones: form.observaciones.trim() || null,
      };
      if (editId) await api.familias.update(editId, payload);
      else await api.familias.create(payload);
      setModal(false);
      load();
    } catch (e) {
      setErrors([e instanceof Error ? e.message : 'Error al guardar']);
    }
  };

  const remove = async (f: Familia) => {
    try {
      await api.familias.delete(f.id);
      setConfirmDelete(null);
      load();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Error al eliminar');
    }
  };

  return (
    <div>
      <div className="page-header">
        <h2>Familias</h2>
        <p>Grupos familiares con cuota mensual compartida</p>
      </div>

      <div className="toolbar">
        <div className="search">
          <input className="form-control" placeholder="Buscar familia..." value={buscar} onChange={e => setBuscar(e.target.value)} />
        </div>
        <button className="btn btn-primary" onClick={openNew}><Plus size={16} /> Nueva Familia</button>
      </div>

      <div className="card table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Cuota mensual</th>
              <th>Socios</th>
              <th>Observaciones</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {familias.map(f => (
              <tr key={f.id}>
                <td><strong>{f.nombre}</strong></td>
                <td>{formatUYU(f.cuotaMensual)}</td>
                <td>{f.cantidadSocios}</td>
                <td className="cell-ellipsis" title={f.observaciones || ''}>{f.observaciones || '—'}</td>
                <td>
                  <button className="btn btn-sm btn-secondary" onClick={() => openEdit(f)}><Edit2 size={14} /></button>
                  <button className="btn btn-sm btn-danger" style={{ marginLeft: 4 }} onClick={() => setConfirmDelete(f)}><Trash2 size={14} /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {familias.length === 0 && <div className="empty-state">No hay familias registradas</div>}
      </div>

      {modal && (
        <div className="modal-overlay" onClick={() => setModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h3>{editId ? 'Editar Familia' : 'Nueva Familia'}</h3>
            {errors.length > 0 && (
              <div className="alert alert-error">
                <ul style={{ margin: 0, paddingLeft: '1.2rem' }}>{errors.map((err, i) => <li key={i}>{err}</li>)}</ul>
              </div>
            )}
            <div className="form-group">
              <label>Nombre de la familia *</label>
              <input className="form-control" maxLength={LIMITS.nombre} value={form.nombre} onChange={e => setForm({ ...form, nombre: e.target.value })} />
            </div>
            <div className="form-group">
              <label>Cuota mensual (UYU) *</label>
              <input className="form-control" type="number" min={1} value={form.cuotaMensual} onChange={e => setForm({ ...form, cuotaMensual: Number(e.target.value) })} />
            </div>
            <div className="form-group">
              <label>Observaciones</label>
              <textarea className="form-control" rows={3} maxLength={LIMITS.notas} value={form.observaciones} onChange={e => setForm({ ...form, observaciones: e.target.value })} />
            </div>
            {editId && (
              <p style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)', marginBottom: '1rem' }}>
                Al cambiar la cuota se actualizará en todos los socios de esta familia (cuota del mes no pagada).
              </p>
            )}
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setModal(false)}>Cancelar</button>
              <button className="btn btn-primary" onClick={save}>Guardar</button>
            </div>
          </div>
        </div>
      )}

      {confirmDelete && (
        <div className="modal-overlay" onClick={() => setConfirmDelete(null)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h3>Eliminar familia</h3>
            <p>¿Eliminar la familia <strong>{confirmDelete.nombre}</strong>?</p>
            {confirmDelete.cantidadSocios > 0 && (
              <p style={{ color: 'var(--color-danger)', fontSize: '0.9rem' }}>
                Solo se puede eliminar si todos sus socios están inactivos.
              </p>
            )}
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setConfirmDelete(null)}>Cancelar</button>
              <button className="btn btn-danger" onClick={() => remove(confirmDelete)}>Eliminar</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
