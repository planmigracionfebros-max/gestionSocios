import { useEffect, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { api } from '../api/client';
import type { GuardarPorteroConfig, PorteroConfig, PorteroPruebaConexion, PorteroSincronizacion } from '../types';
import { useAuth } from '../context/AuthContext';
import { Settings, Wifi, RefreshCw, DoorOpen, Copy, Check } from 'lucide-react';

const emptyForm: GuardarPorteroConfig = {
  habilitado: false,
  apiUrl: 'http://localhost:5000',
  apiKey: '',
  webhookSecret: '',
  deviceSn: '7674222960189',
  sincronizarAutomatico: true,
};

export default function ConfiguracionPorteroPage() {
  const { emisorSlug, isSuperAdmin } = useAuth();
  if (isSuperAdmin) return <Navigate to="/" replace />;
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
      const result = await api.portero.probar(form);
      setPrueba(result);
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
      const result = await api.portero.abrirPuerta();
      setMensaje(result.mensaje);
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

  if (loading) return <div className="loading">Cargando configuración...</div>;

  return (
    <div>
      <div className="page-header">
        <h2><Settings size={22} style={{ verticalAlign: 'middle', marginRight: 8 }} />Configuración portero</h2>
        <p className="text-muted">Conectá ApiPorteroSpa con el control de acceso biométrico ZKTeco de tu spa.</p>
      </div>

      {mensaje && <div className="alert alert-success">{mensaje}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3>Conexión con ApiPortero</h3>
        <p className="text-muted" style={{ marginBottom: '1rem' }}>
          ApiPorteroSpa debe estar corriendo en la PC donde está el portero (puerto 5000 REST, 8081 TCP).
        </p>

        <label className="checkbox-label" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: 8 }}>
          <input
            type="checkbox"
            checked={form.habilitado}
            onChange={e => setForm(f => ({ ...f, habilitado: e.target.checked }))}
          />
          Habilitar integración con portero
        </label>

        <div className="form-grid">
          <div className="form-group">
            <label>URL de ApiPortero</label>
            <input
              type="url"
              value={form.apiUrl}
              onChange={e => setForm(f => ({ ...f, apiUrl: e.target.value }))}
              placeholder="http://192.168.1.100:5000"
            />
            <small className="text-muted">Dirección donde corre run_all.py (sin /api al final)</small>
          </div>

          <div className="form-group">
            <label>API Key (X-API-Key)</label>
            <input
              type="password"
              value={form.apiKey}
              onChange={e => setForm(f => ({ ...f, apiKey: e.target.value }))}
              placeholder="portero-dev-key-change-me"
              autoComplete="off"
            />
            <small className="text-muted">Debe coincidir con PORTERO_API_KEY en ApiPortero</small>
          </div>

          <div className="form-group">
            <label>Serial del dispositivo (SN)</label>
            <input
              type="text"
              value={form.deviceSn}
              onChange={e => setForm(f => ({ ...f, deviceSn: e.target.value }))}
              placeholder="7674222960189"
            />
          </div>

          <div className="form-group">
            <label>Secreto webhook (opcional)</label>
            <input
              type="password"
              value={form.webhookSecret ?? ''}
              onChange={e => setForm(f => ({ ...f, webhookSecret: e.target.value }))}
              placeholder="secreto-compartido"
              autoComplete="off"
            />
            <small className="text-muted">Mismo valor que PORTERO_WEBHOOK_SECRET en ApiPortero</small>
          </div>
        </div>

        <label className="checkbox-label" style={{ marginTop: '1rem', display: 'flex', alignItems: 'center', gap: 8 }}>
          <input
            type="checkbox"
            checked={form.sincronizarAutomatico}
            onChange={e => setForm(f => ({ ...f, sincronizarAutomatico: e.target.checked }))}
          />
          Sincronizar socios automáticamente al crear, editar o dar de baja
        </label>

        <div className="form-actions" style={{ marginTop: '1.25rem', display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <button type="button" className="btn btn-primary" onClick={save} disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar configuración'}
          </button>
          <button type="button" className="btn btn-secondary" onClick={probar} disabled={probando}>
            <Wifi size={16} /> {probando ? 'Probando...' : 'Probar conexión'}
          </button>
        </div>

        {prueba && (
          <div className={`alert ${prueba.ok ? 'alert-success' : 'alert-error'}`} style={{ marginTop: '1rem' }}>
            {prueba.mensaje}
          </div>
        )}

        {fechaActualizacion && (
          <p className="text-muted" style={{ marginTop: '0.75rem', fontSize: '0.85rem' }}>
            Última actualización: {new Date(fechaActualizacion).toLocaleString('es-UY')}
          </p>
        )}
      </div>

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3>Webhook (fichajes en tiempo real)</h3>
        <p className="text-muted">
          Configurá esta URL en ApiPortero como <code>PORTERO_WEBHOOK_URL</code> para recibir fichajes automáticamente.
        </p>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '0.75rem' }}>
          <input type="text" readOnly value={webhookUrl} style={{ flex: 1, fontFamily: 'monospace', fontSize: '0.85rem' }} />
          <button type="button" className="btn btn-sm btn-secondary" onClick={copyWebhook} title="Copiar URL">
            {copied ? <Check size={14} /> : <Copy size={14} />}
          </button>
        </div>
        {emisorSlug && (
          <p className="text-muted" style={{ marginTop: '0.5rem', fontSize: '0.85rem' }}>
            Emisor: <strong>{emisorSlug}</strong> — Los fichajes se registran como ingresos en Gestión Spa.
          </p>
        )}
      </div>

      <div className="card">
        <h3>Acciones</h3>
        <p className="text-muted" style={{ marginBottom: '1rem' }}>
          Los comandos al portero se encolan y el equipo los recibe en ~10 segundos.
        </p>
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <button type="button" className="btn btn-secondary" onClick={sincronizar} disabled={sincronizando || !form.habilitado}>
            <RefreshCw size={16} /> {sincronizando ? 'Sincronizando...' : 'Sincronizar socios activos'}
          </button>
          <button type="button" className="btn btn-secondary" onClick={abrirPuerta} disabled={abriendo || !form.habilitado}>
            <DoorOpen size={16} /> {abriendo ? 'Enviando...' : 'Abrir puerta'}
          </button>
        </div>

        {syncResult && syncResult.errores.length > 0 && (
          <div className="alert alert-error" style={{ marginTop: '1rem' }}>
            <strong>Errores ({syncResult.fallidos}):</strong>
            <ul style={{ margin: '0.5rem 0 0', paddingLeft: '1.25rem' }}>
              {syncResult.errores.map((e, i) => <li key={i}>{e}</li>)}
            </ul>
          </div>
        )}
      </div>

      <div className="card" style={{ marginTop: '1.5rem', background: 'var(--surface-alt, #f8f9fa)' }}>
        <h4 style={{ marginTop: 0 }}>Notas importantes</h4>
        <ul style={{ margin: 0, paddingLeft: '1.25rem', lineHeight: 1.6 }}>
          <li>El PIN en el portero es la <strong>cédula</strong> del socio (solo dígitos).</li>
          <li>Al dar de alta un socio, debés enrolar el rostro manualmente en el equipo.</li>
          <li>Para bloquear acceso por membresía vencida, suspendé o eliminá el socio en Gestión Spa.</li>
          <li>En producción, configurá <code>API_PUBLIC_BASE_URL</code> en Railway con la URL pública de la API.</li>
        </ul>
      </div>
    </div>
  );
}
