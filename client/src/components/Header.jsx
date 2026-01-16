import React from 'react';
import { clsx } from 'clsx';
import { Zap, Menu } from 'lucide-react';

const Header = ({ theme, onMenuClick, onSimulateChaos, chaosMode = false }) => {
    return (
        <header className={clsx(
            "flex flex-col sm:flex-row items-start sm:items-end justify-between",
            "p-4 sm:p-0 mb-6 sm:mb-8 border-b sm:border-b-0 pb-4 sm:pb-4 gap-4 sm:gap-0",
            "shrink-0 transition-colors duration-500",
            chaosMode
                ? "border-red-500/30"
                : (theme === 'dark' ? "border-white/10" : "border-gray-200")
        )}>

            {/* Mobile Top Row: Hamburger + Logo */}
            <div className="flex items-center justify-between w-full sm:w-auto">
                <div className="flex items-center gap-3">
                    {/* Mobile Menu Button */}
                    <button
                        onClick={onMenuClick}
                        className={clsx(
                            "p-2 -ml-2 mr-1 lg:hidden",
                            theme === 'dark' ? "text-white" : "text-gray-700"
                        )}
                    >
                        <Menu size={24} />
                    </button>

                    <img src="/favicon.png" className={clsx("w-8 h-8 sm:w-10 sm:h-10 rounded-full shadow-lg border", chaosMode ? "border-red-500/50" : "border-vedic-gold/20")} alt="VyaaparNexus" />

                    <div className="flex flex-col">
                        {/* Mobile Title */}
                        <h1 className={clsx(
                            "font-orbitron font-bold text-xl sm:text-3xl tracking-tight leading-none sm:mb-1",
                            chaosMode ? "text-red-500" : (theme === 'dark' ? "text-white" : "text-gray-900")
                        )}>
                            <span className="sm:hidden">VYAAPAR NEXUS</span>
                            <span className="hidden sm:inline">SYSTEM <span className={chaosMode ? "text-red-400 animate-pulse" : (theme === 'dark' ? "text-vedic-gold" : "text-deep-maroon")}>OBSERVABILITY</span></span>
                        </h1>

                        {/* Desktop Subtitle (Hidden on mobile to save space, or styled smaller) */}
                        <p className={clsx(
                            "hidden sm:block font-mono text-xs",
                            chaosMode ? "text-red-300/70" : (theme === 'dark' ? "text-gray-400" : "text-gray-600")
                        )}>
                            VYAAPAR NEXUS CORE // V.2.0.5 // <span className={chaosMode ? "text-red-500 animate-pulse font-bold" : "text-green-500 animate-pulse"}>{chaosMode ? "⚠️ CRITICAL FAILURE SIMULATION" : "LIVE SIGNAL"}</span>
                        </p>
                    </div>
                </div>
            </div>

            {/* Mobile Subtitle (Visible only on mobile, below logo) */}
            <div className="sm:hidden w-full">
                <p className={clsx(
                    "font-mono text-[10px]",
                    chaosMode ? "text-red-300/70" : (theme === 'dark' ? "text-gray-400" : "text-gray-600")
                )}>
                    V.2.0.5 // <span className={chaosMode ? "text-red-500 animate-pulse font-bold" : "text-green-500 animate-pulse"}>{chaosMode ? "⚠️ CRITICAL" : "LIVE SIGNAL"}</span>
                </p>
            </div>

            {/* Controls */}
            <div className="flex flex-row items-center justify-between w-full sm:w-auto gap-4">
                <button
                    onClick={onSimulateChaos}
                    className={clsx(
                        "group relative flex-1 sm:flex-initial px-4 py-2 rounded-lg overflow-hidden transition-all text-center justify-center border",
                        chaosMode
                            ? "bg-red-600 text-white shadow-lg shadow-red-500/50 animate-pulse border-red-400"
                            : "bg-red-500/10 hover:bg-red-500/20 text-red-400 border-red-500/30"
                    )}
                >
                    <span className="relative z-10 font-orbitron text-xs tracking-widest flex items-center justify-center gap-2">
                        <Zap size={14} className={clsx(chaosMode ? "fill-current animate-[spin_1s_linear_infinite]" : "group-hover:text-red-300")} />
                        <span className="whitespace-nowrap">{chaosMode ? "CHAOS ACTIVE" : "SIMULATE CHAOS"}</span>
                    </span>
                    {!chaosMode && (
                        <div className="absolute inset-0 bg-red-500/10 translate-y-full group-hover:translate-y-0 transition-transform duration-300" />
                    )}
                </button>

                <div className={clsx(
                    "text-right pl-6 border-l shrink-0 transition-colors",
                    chaosMode
                        ? "border-red-500/30"
                        : (theme === 'dark' ? "border-white/10" : "border-gray-200")
                )}>
                    <div className={clsx(
                        "text-[10px] sm:text-xs uppercase tracking-wider",
                        chaosMode ? "text-red-300/70" : (theme === 'dark' ? "text-gray-500" : "text-gray-600")
                    )}> uptime </div>
                    <div className={clsx(
                        "font-mono text-sm sm:text-xl",
                        chaosMode ? "text-red-400" : (theme === 'dark' ? "text-white" : "text-gray-900")
                    )}>14d 02h 12m</div>
                </div>
            </div>
        </header>
    );
};

export default Header;
