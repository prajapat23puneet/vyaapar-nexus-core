import { useState, useEffect } from 'react';
import { clsx } from 'clsx';
import { ShoppingCart, Activity, AlertTriangle, Menu, Zap } from 'lucide-react';
import Sidebar from './components/Sidebar';
import Header from './components/Header';
import MetricCard from './components/MetricCard';
import ServiceMesh from './components/ServiceMesh';
import LogTerminal from './components/LogTerminal';
import { useSystemStream } from './hooks/useSystemStream';
import './App.css';

function App() {
  const [theme, setTheme] = useState('dark');
  const [isSidebarOpen, setSidebarOpen] = useState(false);

  // Chaos Mode State
  const [chaosMode, setChaosMode] = useState(false);
  const [chaosIntensity, setChaosIntensity] = useState(0);

  // Destructure state, actions, and metrics
  const { state: systemState, actions, metrics } = useSystemStream();

  // Toggle Theme
  const toggleTheme = () => {
    const newTheme = theme === 'dark' ? 'light' : 'dark';
    setTheme(newTheme);
    document.documentElement.setAttribute('data-theme', newTheme);
  };

  // Toggle Chaos Mode
  const toggleChaos = () => {
    const newMode = !chaosMode;
    setChaosMode(newMode);

    // Trigger system chaos simulation
    actions.simulateChaos();

    // Visual effect intensity transition
    if (newMode) {
      // Start chaos - gradually increase intensity
      let intensity = 0;
      const interval = setInterval(() => {
        intensity += 0.1;
        if (intensity >= 1) {
          intensity = 1;
          clearInterval(interval);
        }
        setChaosIntensity(intensity);
      }, 100);
    } else {
      // Stop chaos - gradually decrease intensity
      let intensity = chaosIntensity;
      const interval = setInterval(() => {
        intensity -= 0.1;
        if (intensity <= 0) {
          intensity = 0;
          clearInterval(interval);
        }
        setChaosIntensity(intensity);
      }, 100);
    }
  };

  // Set initial theme and chaos variables
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', 'dark');
  }, []);

  // Update CSS variables for chaos
  useEffect(() => {
    if (chaosMode) {
      document.documentElement.style.setProperty('--chaos-intensity', chaosIntensity);
      document.documentElement.style.setProperty('--chaos-red', `rgba(239, 68, 68, ${0.3 * chaosIntensity})`);
      document.documentElement.style.setProperty('--chaos-orange', `rgba(234, 88, 12, ${0.2 * chaosIntensity})`);
      document.body.classList.add('chaos-mode');
    } else {
      document.documentElement.style.setProperty('--chaos-intensity', '0');
      document.body.classList.remove('chaos-mode');
    }

    return () => {
      document.body.classList.remove('chaos-mode');
    };
  }, [chaosMode, chaosIntensity]);

  return (
    <div
      className={clsx(
        "min-h-screen w-full transition-all duration-1000 ease-in-out font-inter overflow-hidden",
        chaosMode ? 'bg-chaos-active' : '',
        theme === 'dark' ? "bg-cosmic-black text-white" : "bg-paper-white text-[#1a1a1a]"
      )}
      style={{
        background: chaosMode
          ? `
            radial-gradient(circle at 20% 80%, rgba(220, 38, 38, ${0.15 * chaosIntensity}) 0%, transparent 50%),
            radial-gradient(circle at 80% 20%, rgba(234, 88, 12, ${0.12 * chaosIntensity}) 0%, transparent 50%),
            radial-gradient(circle at 40% 40%, rgba(202, 138, 4, ${0.1 * chaosIntensity}) 0%, transparent 50%),
            radial-gradient(circle at 60% 60%, rgba(239, 68, 68, ${0.13 * chaosIntensity}) 0%, transparent 50%),
            #0a0a0a
          `
          : undefined
      }}
    >
      {/* Background Decor (Hide in chaos mode for cleaner look or overlay?) - Keeping for now but reducing opacity */}
      <div className={clsx(
        "fixed inset-0 z-0 pointer-events-none overflow-hidden transition-opacity duration-500",
        chaosMode ? "opacity-30" : "opacity-100"
      )}>
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

      {/* Animated overlay particles for Chaos Mode */}
      {chaosMode && (
        <div className="fixed inset-0 overflow-hidden pointer-events-none z-0">
          {[...Array(20)].map((_, i) => (
            <div
              key={i}
              className="absolute w-1 h-1 bg-red-500/50 rounded-full animate-float"
              style={{
                left: `${Math.random() * 100}%`,
                top: `${Math.random() * 100}%`,
                animationDelay: `${Math.random() * 3}s`,
                animationDuration: `${3 + Math.random() * 4}s`,
                opacity: chaosIntensity * 0.6,
              }}
            />
          ))}
        </div>
      )}

      <Sidebar
        isOpen={isSidebarOpen}
        toggle={() => setSidebarOpen(!isSidebarOpen)}
        theme={theme}
        toggleTheme={toggleTheme}
        chaosMode={chaosMode}
      />

      {/* Main Content */}
      <main className={clsx(
        "relative z-10 transition-all duration-300 flex flex-col h-screen overflow-hidden",
        "lg:ml-64" // Offset for sidebar
      )}>
        <header className={clsx(
          "border-b p-4 sm:p-6 transition-all duration-500",
          chaosMode
            ? "glass-chaos border-red-500/30"
            : "glass border-gray-800/50"
        )}>
          <Header
            theme={theme}
            onMenuClick={() => setSidebarOpen(true)}
            onSimulateChaos={toggleChaos}
            chaosMode={chaosMode}
          />
        </header>

        {/* Dashboard Grid */}
        <div className="flex-1 min-h-0 p-4 md:p-8 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6 pb-20 md:pb-8 overflow-y-auto">

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
            chaosMode={chaosMode}
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
            chaosMode={chaosMode}
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
            chaosMode={chaosMode}
          />

          {/* Quick Status */}
          <div className={clsx(
            "glass-panel p-6 rounded-xl flex flex-col justify-center items-center relative overflow-hidden transition-all duration-500",
            chaosMode ? "bg-gray-900/40 border-red-500/30 shadow-lg shadow-red-500/10" : ""
          )}>
            <div className={clsx(
              "text-xs uppercase tracking-widest mb-2 z-10",
              theme === 'dark' ? "text-gray-400" : "text-gray-600"
            )}>System Status</div>
            <div className={clsx(
              "text-2xl font-orbitron font-bold z-10 transition-colors duration-500",
              chaosMode
                ? "text-red-400"
                : systemState.Metrics.DeadLetters > 10
                  ? "text-red-500 animate-pulse"
                  : theme === 'dark'
                    ? "text-green-400"
                    : "text-green-600"
            )}>
              {chaosMode ? "CRITICAL" : (systemState.Metrics.DeadLetters > 10 ? "CRITICAL" : "OPERATIONAL")}
            </div>

            {/* Status Bars */}
            <div className="mt-4 flex gap-2 z-10">
              {[1, 2, 3, 4].map(i => (
                <div key={i}
                  className={clsx(
                    "w-2 h-8 rounded-sm animate-pulse transition-all duration-500",
                    chaosMode
                      ? "bg-red-400 shadow-[0_0_10px_rgba(248,113,113,0.5)]"
                      : systemState.Metrics.DeadLetters > 10
                        ? theme === 'dark' ? "bg-red-500/50" : "bg-red-500/60"
                        : theme === 'dark' ? "bg-green-500/50" : "bg-green-500/60"
                  )}
                  style={{ animationDelay: chaosMode ? `${i * 0.2}s` : `${i * 0.1}s` }}
                />
              ))}
            </div>
          </div>

          {/* Row 2: Service Mesh (Span 2 cols, 2 rows) */}
          <div className="md:col-span-2 md:row-span-2 min-h-[300px]">
            <ServiceMesh services={systemState.Services} theme={theme} chaosMode={chaosMode} />
          </div>

          {/* Row 2: Logs (Span 2 cols, 2 rows) - Fixed height for scrolling */}
          <div className="md:col-span-2 md:row-span-2 h-[300px] sm:h-[400px] lg:h-[500px]">
            <LogTerminal logs={systemState.Logs} theme={theme} chaosMode={chaosMode} />
          </div>

        </div>
      </main>
    </div>
  );
}

export default App;
