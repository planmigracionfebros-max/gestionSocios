import { useEffect, useState } from 'react';
import { api } from '../api/client';
import type { GuardarPorteroConfig, PorteroConfig, PorteroPruebaConexion, PorteroSincronizacion } from '../types';
import { useAuth } from '../context/AuthContext';
import { Wifi, RefreshCw, DoorOpen, Copy, Check } from 'lucide-react';

const emptyForm: GuardarPorteroConfig = {
  habilitado: false,
  apiUrl: 'http://localhost:5000',
  apiKey: '',
  webhookSecret: '',
  deviceSn: '7674222960189',
  sincronizarAutomatico: true,
};

export default function PorteroConfigSection() {
  const { emisorSlug } = useAuth();
  const [form, setForm] = useState<GuardarPorteroConfig>(emptyForm);
  const [webhookUrl, setWebhookUrl] = useState('');
  const [fechaActualizacion, setFechaActualizacion] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [probando, setProbando] = useState(false);
  const [sincronizando, setSincronizando] = useState(false);
  const [abriendo, setAbriendo] = useState(false);
  const [mensaje, setMensaje] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [prueba, setPrueba] = useState<PorteroPruebaConexion | null>(null);
  const [syncResult, setSyncResult] = useState<PorteroSincronizacion | null>(null);
  const [copied, setCopied] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const cfg: PorteroConfig = await api.portero.getConfig();
      setForm({
        habilitado: cfg.habilitado,
        apiUrl: cfg.apiUrl,
        apiKey: cfg.apiKey,
        webhookSecret: cfg.webhookSecret || '',
        deviceSn: cfg.deviceSn,
        sincronizarAutomatico: cfg.sincronizarAutomatico,
      });
      setWebhookUrl(cfg.webhookUrl);
      setFechaActualizacion(cfg.fechaActualizacion);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al cargar configuración');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const save = async () => {
    setSaving(true);
    setError(null);
    setMensaje(null);
    try {
      const cfg = await api.portero.saveConfig({
        ...form,
        webhookSecret: form.webhookSecret?.trim() || null,
      });
      setWebhookUrl(cfg.webhookUrl);
      setFechaActualizacion(cfg.fechaActualizacion);
      setMensaje('Configuración guardada correctamente');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al guardar');
    } finally {
      setSaving(false);
    }
  };

  const probar = async () => {
    setProbando(true);
    setError(null);
    setPrueba(null);
    try {
      setPrueba(await api.portero.probar(form));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al probar conexión');
    } finally {
      setProbando(false);
    }
  };

  const sincronizar = async () => {
    setSincronizando(true);
    setError(null);
    setSyncResult(null);
    try {
      const result = await api.portero.sincronizar();
      setSyncResult(result);
      setMensaje(`Sincronización: ${result.exitosos}/${result.total} socios enviados al portero`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al sincronizar');
    } finally {
      setSincronizando(false);
    }
  };

  const abrirPuerta = async () => {
    setAbriendo(true);
    setError(null);
    try {
      setMensaje((await api.portero.abrirPuerta()).mensaje);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error al abrir puerta');
    } finally {
      setAbriendo(false);
    }
  };

  const copyWebhook = async () => {
    if (!webhookUrl) return;
    await navigator.clipboard.writeText(webhookUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (loading) return <div className="loading">Cargando configuración del portero...</div>;

  return (
    <section id="portero">
      {mensaje && <div className="alert alert-success">{mensaje}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3>Portero biométrico (ApiPorteroSpa)</h3>
        <p className="text-muted" style={{ marginBottom: '1rem' }}>
          Conectá el control de acceso ZKTeco. ApiPortero debe correr en la PC del portero (puerto 5000 REST, 8081 TCP).
        </p>

        <label className="checkbox-label" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={form.habilitado} onChange={e => setForm(f => ({ ...f, habilitado: e.target.checked }))} />
          Habilitar integración con portero
        </label>

        <div className="form-grid">
          <div className="form-group">
            <label>URL de ApiPortero</label>
            <input type="url" value={form.apiUrl} onChange={e => setForm(f => ({ ...f, apiUrl: e.target.value }))} placeholder="http://192.168.1.100:5000" />
          </div>
          <div className="form-group">
            <label>API Key (X-API-Key)</label>
            <input type="password" value={form.apiKey} onChange={e => setForm(f => ({ ...f, apiKey: e.target.value }))} autoComplete="off" />
          </div>
          <div className="form-group">
            <label>Serial del dispositivo (SN)</label>
            <input type="text" value={form.deviceSn} onChange={e => setForm(f => ({ ...f, deviceSn: e.target.value }))} />
          </div>
          <div className="form-group">
            <label>Secreto webhook (opcional)</label>
            <input type="password" value={form.webhookSecret ?? ''} onChange={e => setForm(f => ({ ...f, webhookSecret: e.target.value }))} autoComplete="off" />
          </div>
        </div>

        <label className="checkbox-label" style={{ marginTop: '1rem', display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={form.sincronizarAutomatico} onChange={e => setForm(f => ({ ...f, sincronizarAutomatico: e.target.checked }))} />
          Sincronizar socios automáticamente al crear, editar o dar de baja
        </label>

        <div className="form-actions" style={{ marginTop: '1.25rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <button type="button" className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button>
          <button type="button" className="btn btn-secondary" onClick={probar} disabled={probando}><Wifi size={16} /> {probando ? 'Probando...' : 'Probar conexión'}</button>
        </div>

        {prueba && <div className={`alert ${prueba.ok ? 'alert-success' : 'alert-error'}`} style={{ marginTop: '1rem' }}>{prueba.mensaje}</div>}
        {fechaActualizacion && <p className="text-muted" style={{ marginTop: '0.75rem', fontSize: '0.85rem' }}>Última actualización: {new Date(fechaActualizacion).toLocaleString('es-UY')}</p>}
      </div>

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3>Webhook de fichajes</h3>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '0.75rem' }}>
          <input type="text" readOnly value={webhookUrl} style={{ flex: 1, fontFamily: 'monospace', fontSize: '0.85rem' }} />
          <button type="button" className="btn btn-sm btn-secondary" onClick={copyWebhook}>{copied ? <Check size={14} /> : <Copy size={14} />}</button>
        </div>
        {emisorSlug && <p className="text-muted" style={{ marginTop: '0.5rem', fontSize: '0.85rem' }}>Emisor: <strong>{emisorSlug}</strong></p>}
      </div>

      <div className="card">
        <h3>Acciones del portero</h3>
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', marginTop: '0.75rem' }}>
          <button type="button" className="btn btn-secondary" onClick={sincronizar} disabled={sincronizando || !form.habilitado}><RefreshCw size={16} /> {sincronizando ? 'Sincronizando...' : 'Sincronizar socios activos'}</button>
          <button type="button" className="btn btn-secondary" onClick={abrirPuerta} disabled={abriendo || !form.habilitado}><DoorOpen size={16} /> {abriendo ? 'Enviando...' : 'Abrir puerta'}</button>
        </div>
        {syncResult && syncResult.errores.length > 0 && (
          <div className="alert alert-error" style={{ marginTop: '1rem' }}>
            <strong>Errores ({syncResult.fallidos}):</strong>
            <ul style={{ margin: '0.5rem 0 0', paddingLeft: '1.25rem' }}>{syncResult.errores.map((e, i) => <li key={i}>{e}</li>)}</ul>
          </div>
        )}
      </div>
    </section>
  );
}
