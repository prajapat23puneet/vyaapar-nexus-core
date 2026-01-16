import { useState, useEffect } from 'react';
import { clsx } from 'clsx';
import { ShoppingCart, Activity, AlertTriangle, Menu, Zap } from 'lucide-react';
import Sidebar from './components/Sidebar';
import Header from './components/Header';
import MetricCard from './components/MetricCard';
import ServiceMesh from './components/ServiceMesh';
import LogTerminal from './components/LogTerminal';
import { useSystemStream } from './hooks/useSystemStream';

function App() {
  const [theme, setTheme] = useState('dark');
  const [isSidebarOpen, setSidebarOpen] = useState(false);

  // Destructure state, actions, and metrics
  const { state: systemState, actions, metrics } = useSystemStream();

  // Toggle Theme
  const toggleTheme = () => {
    const newTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
    document.documentElement.setAttribute('data-theme', newTheme);
  };

  // Set initial theme
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', 'dark');
  }, []);

  return (
    <div className={clsx(
      "min-h-screen w-full transition-colors duration-300 font-inter",
      theme === 'dark' ? "bg-cosmic-black text-white" : "bg-paper-white text-[#1a1a1a]"
    )}>
      {/* Background Decor */}
      <div className="fixed inset-0 z-0 pointer-events-none overflow-hidden">
        <div className="absolute inset-0 opacity-[0.03]"
          style={{ backgroundImage: `radial-gradient(${theme === 'dark' ? '#fff' : '#000'} 1px, transparent 1px)`, backgroundSize: '32px 32px' }}
        />

        <div className={clsx(
          "absolute top-1/4 right-1/4 w-96 h-96 rounded-full blur-[128px] opacity-20",
          theme === 'dark' ? "bg-vedic-gold" : "bg-deep-maroon"
        )} />
        <div className={clsx(
          "absolute bottom-0 left-0 w-[500px] h-[500px] rounded-full blur-[128px] opacity-10",
          theme === 'dark' ? "bg-neon-teal" : "bg-vedic-gold"
        )} />
      </div>

      <Sidebar
        isOpen={isSidebarOpen}
        toggle={() => setSidebarOpen(!isSidebarOpen)}
        theme={theme}
        toggleTheme={toggleTheme}
      />

      {/* Main Content */}
      <main className={clsx(
        "relative z-10 transition-all duration-300 p-4 md:p-8 flex flex-col h-screen overflow-hidden",
        "lg:ml-64" // Offset for sidebar
      )}>
        <Header
          theme={theme}
          onMenuClick={() => setSidebarOpen(true)}
          onSimulateChaos={actions.simulateChaos}
        />

        {/* Dashboard Grid */}
        <div className="flex-1 min-h-0 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6 pb-20 md:pb-0 overflow-y-auto">

          {/* Row 1: Metrics */}
          <MetricCard
            title="Orders / Sec"
            value={metrics?.OrdersPerSec?.value ?? systemState.Metrics.OrdersPerSec}
            icon={ShoppingCart}
            trend={metrics?.OrdersPerSec?.trend}
            history={metrics?.OrdersPerSec?.history ?? []}
            color="gold"
            theme={theme}
            metricType="orders"
          />
          <MetricCard
            title="Active Sagas"
            value={metrics?.ActiveSagas?.value ?? systemState.Metrics.ActiveSagas}
            icon={Activity}
            trend={metrics?.ActiveSagas?.trend}
            history={metrics?.ActiveSagas?.history ?? []}
            color="teal"
            theme={theme}
            metricType="sagas"
          />
          <MetricCard
            title="Dead Letters"
            value={metrics?.DeadLetters?.value ?? systemState.Metrics.DeadLetters}
            icon={AlertTriangle}
            trend={metrics?.DeadLetters?.trend}
            history={metrics?.DeadLetters?.history ?? []}
            color="maroon"
            theme={theme}
            metricType="deadLetters"
          />

          {/* Quick Status */}
          <div className="glass-panel p-6 rounded-xl flex flex-col justify-center items-center relative overflow-hidden">
            <div className={clsx(
              "text-xs uppercase tracking-widest mb-2 z-10",
              theme === 'dark' ? "text-gray-400" : "text-gray-600"
            )}>System Status</div>
            <div className={clsx(
              "text-2xl font-orbitron font-bold z-10 transition-colors duration-500",
              systemState.Metrics.DeadLetters > 10
                ? "text-red-500 animate-pulse"
                : theme === 'dark'
                  ? "text-green-400"
                  : "text-green-600"
            )}>
              {systemState.Metrics.DeadLetters > 10 ? "CRITICAL" : "OPERATIONAL"}
            </div>

            {/* Status Bars */}
            <div className="mt-4 flex gap-2 z-10">
              {[1, 2, 3, 4].map(i => (
                <div key={i}
                  className={clsx(
                    "w-2 h-8 rounded-sm animate-pulse transition-colors duration-500",
                    systemState.Metrics.DeadLetters > 10
                      ? theme === 'dark' ? "bg-red-500/50" : "bg-red-500/60"
                      : theme === 'dark' ? "bg-green-500/50" : "bg-green-500/60"
                  )}
                  style={{ animationDelay: `${i * 0.1}s` }}
                />
              ))}
            </div>
          </div>

          {/* Row 2: Service Mesh (Span 2 cols, 2 rows) */}
          <div className="md:col-span-2 md:row-span-2 min-h-[300px]">
            <ServiceMesh services={systemState.Services} theme={theme} />
          </div>

          {/* Row 2: Logs (Span 2 cols, 2 rows) - Fixed height for scrolling */}
          <div className="md:col-span-2 md:row-span-2 h-[300px] sm:h-[400px] lg:h-[500px]">
            <LogTerminal logs={systemState.Logs} theme={theme} />
          </div>

        </div>
      </main>
    </div>
  );
}

export default App;
