import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import {
  LayoutDashboard, Users, UserPlus, Sparkles, Receipt,
  BarChart3, DoorOpen, CreditCard, Menu, ChevronsLeft
} from 'lucide-react';

const navItems = [
  { to: '/', icon: LayoutDashboard, label: 'Panel' },
  { to: '/socios', icon: Users, label: 'Socios' },
  { to: '/clientes', icon: UserPlus, label: 'Clientes' },
  { to: '/servicios', icon: Sparkles, label: 'Servicios' },
  { to: '/cargos', icon: Receipt, label: 'Cargos' },
  { to: '/cuotas', icon: CreditCard, label: 'Cuotas' },
  { to: '/informes', icon: BarChart3, label: 'Informes' },
  { to: '/ingreso', icon: DoorOpen, label: 'Control de Ingreso' },
];

export default function Layout() {
  const [sidebarOpen, setSidebarOpen] = useState(true);

  return (
    <div className={`app-layout${sidebarOpen ? '' : ' sidebar-collapsed'}`}>
      <aside className="sidebar" aria-hidden={!sidebarOpen}>
        <button
          type="button"
          className="sidebar-toggle sidebar-toggle--collapse"
          onClick={() => setSidebarOpen(false)}
          aria-label="Ocultar menú"
          title="Ocultar menú"
        >
          <ChevronsLeft size={18} />
        </button>
        <div className="sidebar-brand">
          <h1>SPA Thermal Daymán</h1>
          <p>Termas del Daymán · Salto, Uruguay</p>
        </div>
        <nav className="sidebar-nav">
          {navItems.map(({ to, icon: Icon, label }) => (
            <NavLink key={to} to={to} end={to === '/'} className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="main-column">
        <header className="top-bar">
          <button
            type="button"
            className="sidebar-toggle sidebar-toggle--expand"
            onClick={() => setSidebarOpen(true)}
            aria-label="Mostrar menú"
            title="Mostrar menú"
          >
            <Menu size={20} />
          </button>
          <span className="top-bar-brand">SPA Thermal Daymán</span>
        </header>
        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
