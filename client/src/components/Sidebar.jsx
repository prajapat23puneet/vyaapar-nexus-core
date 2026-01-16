import React, { useState } from 'react';
import { LayoutDashboard, Server, Settings, Activity, Menu, X, Shield, Bell } from 'lucide-react';
import { clsx } from 'clsx';
import { motion, AnimatePresence } from 'framer-motion';

const Sidebar = ({ isOpen, toggle, theme, toggleTheme, chaosMode = false }) => {
    const menuItems = [
        { icon: LayoutDashboard, label: 'Overview', active: true },
        { icon: Server, label: 'Services' },
        { icon: Activity, label: 'Traffic' },
        { icon: Shield, label: 'Security' },
        { icon: Settings, label: 'Settings' },
    ];

    return (
        <>
            {/* Mobile Overlay */}
            <AnimatePresence>
                {isOpen && (
                    <motion.div
                        initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                        className={clsx(
                            "fixed inset-0 z-40 lg:hidden backdrop-blur-md transition-opacity duration-300",
                            chaosMode ? "bg-black/60" : "bg-black/40"
                        )}
                        onClick={toggle}
                    />
                )}
            </AnimatePresence>

            {/* Sidebar Container */}
            <motion.div
                className={clsx(
                    "fixed top-0 left-0 h-full z-50 lg:z-auto w-64 border-r flex flex-col transition-all duration-500 sidebar-scrollbar overflow-y-auto",
                    chaosMode
                        ? "glass-chaos-strong border-red-500/40"
                        : "glass-strong border-gray-700/50",
                    isOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
                )}
                style={{
                    backdropFilter: chaosMode ? 'blur(20px) saturate(200%)' : 'blur(20px) saturate(180%)',
                    WebkitBackdropFilter: chaosMode ? 'blur(20px) saturate(200%)' : 'blur(20px) saturate(180%)',
                }}
            >
                {/* Header */}
                <div className={clsx(
                    "p-6 flex items-center justify-between border-b transition-colors duration-300",
                    chaosMode ? "border-red-500/30" : "border-gray-800/50"
                )}>
                    <div className="flex items-center gap-3">
                        <img
                            src="/favicon.png"
                            className={clsx(
                                "w-8 h-8 rounded-full shadow-lg border transition-colors",
                                chaosMode ? "border-red-500/50" : (theme === 'dark' ? "border-vedic-gold/50" : "border-vedic-gold/30")
                            )}
                            alt="VyaaparNexus"
                        />
                        <h1 className={clsx(
                            "font-orbitron font-bold text-lg tracking-wider transition-colors",
                            chaosMode
                                ? "text-red-500"
                                : (theme === 'dark'
                                    ? "text-transparent bg-clip-text bg-gradient-to-r from-vedic-gold to-white"
                                    : "text-transparent bg-clip-text bg-gradient-to-r from-deep-maroon to-gray-800")
                        )}>
                            VYAAPAR
                        </h1>
                    </div>
                    <button onClick={toggle} className="lg:hidden text-gray-400 hover:text-white">
                        <X size={24} className={chaosMode ? "text-red-400" : ""} />
                    </button>
                </div>

                {/* Navigation */}
                <nav className="flex-1 px-4 space-y-2 mt-8">
                    {menuItems.map((item) => (
                        <button
                            key={item.label}
                            className={clsx(
                                "w-full flex items-center gap-4 px-4 py-3 rounded-lg transition-all duration-300 group text-sm font-medium",
                                item.active
                                    ? chaosMode
                                        ? "glass-chaos text-red-400 border-red-500/30"
                                        : (theme === 'dark'
                                            ? "glass bg-vedic-gold/20 text-vedic-gold border-vedic-gold/20"
                                            : "glass-light bg-deep-maroon/10 text-deep-maroon border-deep-maroon/30")
                                    : chaosMode
                                        ? "text-gray-400 hover:text-white hover:glass-chaos hover:border-red-500/30"
                                        : (theme === 'dark'
                                            ? "text-gray-400 hover:text-white hover:glass-light"
                                            : "text-gray-600 hover:text-gray-900 hover:glass-light")
                            )}
                        >
                            <item.icon
                                size={20}
                                className={clsx(
                                    item.active
                                        ? (chaosMode ? "text-red-500" : (theme === 'dark' ? "text-vedic-gold" : "text-deep-maroon"))
                                        : (chaosMode
                                            ? "text-gray-500 group-hover:text-red-400"
                                            : (theme === 'dark' ? "text-gray-500 group-hover:text-white" : "text-gray-500 group-hover:text-gray-900"))
                                )}
                            />
                            {item.label}
                            {item.label === 'Security' && <span className={clsx("ml-auto w-2 h-2 rounded-full", chaosMode ? "bg-red-600 animate-pulse" : "bg-red-500")} />}
                        </button>
                    ))}
                </nav>

                {/* Footer actions */}
                <div className={clsx(
                    "p-4 border-t space-y-4 mt-auto transition-colors duration-300",
                    chaosMode ? "border-red-500/30 glass-chaos" : "border-gray-800/50 glass-light"
                )}>
                    <div className="flex items-center gap-4 px-4">
                        <button
                            onClick={toggleTheme}
                            className={clsx(
                                "p-2 rounded-full transition-colors",
                                theme === 'dark'
                                    ? "bg-white/5 hover:bg-white/10 text-yellow-400"
                                    : "bg-gray-100 hover:bg-gray-200 text-yellow-600"
                            )}
                            title="Toggle Theme"
                        >
                            {/* Simple toggle visual */}
                            <div className="w-4 h-4 rounded-full border-2 border-current" />
                        </button>
                        <div className={clsx(
                            "text-xs font-mono",
                            theme === 'dark' ? "text-gray-500" : "text-gray-600"
                        )}>
                            v2.0.5 <span className={clsx(chaosMode ? "text-red-500 animate-pulse" : "text-green-500")}>{chaosMode ? "CRITICAL" : "ONLINE"}</span>
                        </div>
                    </div>
                </div>
            </motion.div>
        </>
    );
};

export default Sidebar;
