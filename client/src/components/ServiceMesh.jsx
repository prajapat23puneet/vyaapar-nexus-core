import React from 'react';
import { motion } from 'framer-motion';
import { clsx } from 'clsx';

const ServiceMesh = ({ services, theme = 'dark', chaosMode = false }) => {
    const radius = 120;
    const center = { x: 200, y: 150 };

    return (
        <div className={clsx(
            "p-4 rounded-xl h-full flex flex-col relative overflow-hidden transition-all duration-500",
            chaosMode
                ? "glass-chaos border-orange-500/30 shadow-lg shadow-orange-500/10"
                : "glass border-gray-700/40"
        )}>
            <h3 className={clsx(
                "font-orbitron text-sm mb-4 tracking-wider uppercase flex justify-between",
                chaosMode ? "text-orange-400" : (theme === 'dark' ? "text-gray-400" : "text-gray-700")
            )}>
                Nexus Service Mesh
                <span className={clsx(
                    "text-[10px] font-mono",
                    chaosMode ? "text-red-400 animate-pulse" : (theme === 'dark' ? "text-gray-600" : "text-gray-500")
                )}>
                    {chaosMode ? "⚠️ UNSTABLE TOPOLOGY" : "LIVE TOPOLOGY"}
                </span>
            </h3>

            <div className="flex-1 relative flex items-center justify-center overflow-hidden">
                {/* Scaled Container for Mobile */}
                <div className="w-full h-full relative flex items-center justify-center scale-75 sm:scale-90 lg:scale-100 origin-center transition-transform duration-300">
                    {/* SVG Container */}
                    <svg width="400" height="300" viewBox="0 0 400 300" className="overflow-visible">
                        <defs>
                            <filter id="glow">
                                <feGaussianBlur stdDeviation="2.5" result="coloredBlur" />
                                <feMerge>
                                    <feMergeNode in="coloredBlur" />
                                    <feMergeNode in="SourceGraphic" />
                                </feMerge>
                            </filter>
                            <linearGradient id="lineGap" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="100%" y2="0">
                                <stop offset="0%" stopColor="transparent" />
                                <stop offset="50%" stopColor="white" />
                                <stop offset="100%" stopColor="transparent" />
                            </linearGradient>
                        </defs>

                        {/* Central Hub */}
                        <circle
                            cx={center.x}
                            cy={center.y}
                            r="30"
                            fill={chaosMode ? "#1a0500" : (theme === 'dark' ? "#0a0a0f" : "#ffffff")}
                            stroke={chaosMode ? "#ef4444" : "#FFD700"}
                            strokeWidth="2"
                            filter="url(#glow)"
                            style={theme === 'light' ? { filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.1))' } : {}}
                            className={chaosMode ? "animate-pulse" : ""}
                        />
                        <g className={clsx("origin-[200px_150px]", chaosMode ? "animate-[spin_2s_linear_infinite]" : "animate-[spin_10s_linear_infinite]")}>
                            <circle cx={center.x} cy={center.y} r="35" fill="none" stroke={chaosMode ? "#f97316" : "#FFD700"} strokeWidth="1" strokeDasharray="5,5" opacity="0.3" />
                        </g>
                        <text
                            x={center.x}
                            y={center.y}
                            dy="5"
                            textAnchor="middle"
                            fill={chaosMode ? "#ef4444" : (theme === 'dark' ? "#FFD700" : "#800000")}
                            fontSize="10"
                            className="font-orbitron font-bold"
                            style={theme === 'light' ? { filter: 'drop-shadow(0 1px 2px rgba(0,0,0,0.1))' } : {}}
                        >
                            CORE
                        </text>

                        {services.map((svc, i) => {
                            const angle = (i / services.length) * 2 * Math.PI - Math.PI / 2;
                            const x = center.x + radius * Math.cos(angle);
                            const y = center.y + radius * Math.sin(angle);

                            const isUp = svc.Status === 'UP';
                            const isDegraded = svc.Status === 'DEGRADED';
                            const isDown = svc.Status === 'DOWN';

                            let statusColor = theme === 'dark' ? '#00f3ff' : '#0066cc';
                            if (isDegraded) statusColor = '#FFD700'; // Gold
                            if (isDown) statusColor = '#ff0000';    // Red

                            if (chaosMode) {
                                // In chaos mode, things look unstable
                                const random = Math.random();
                                if (random > 0.7) statusColor = '#ef4444'; // Red
                                else if (random > 0.4) statusColor = '#f97316'; // Orange
                            }

                            return (
                                <g key={`node-${svc.Id}`}>
                                    {/* Connection Line */}
                                    <line
                                        x1={center.x} y1={center.y}
                                        x2={x} y2={y}
                                        stroke={isDown || chaosMode ? '#ef4444' : (theme === 'dark' ? 'rgba(255, 255, 255, 0.1)' : 'rgba(0, 0, 0, 0.1)')}
                                        strokeWidth={isDown || chaosMode ? 2 : 1}
                                        strokeDasharray={isDown || chaosMode ? "5,5" : "0"}
                                        className={isDown || chaosMode ? "animate-pulse" : ""}
                                    />

                                    {/* Animated Packet - Subtle Enterprise Flow */}
                                    {(!isDown) && (
                                        <circle r={isDegraded || chaosMode ? 2 : 2} fill={statusColor}>
                                            <animateMotion
                                                dur={`${isDegraded || chaosMode ? 2 : 4}s`}
                                                repeatCount="indefinite"
                                                path={`M${center.x},${center.y} L${x},${y} L${center.x},${center.y}`}
                                                keyPoints="0;0.5;1;0.5;0"
                                                keyTimes="0;0.25;0.5;0.75;1"
                                            />
                                            {/* Gentle Opacity Pulse */}
                                            <animate attributeName="opacity" values="0.4;1;0.4" dur="2s" repeatCount="indefinite" />
                                        </circle>
                                    )}

                                    {/* Node Circle */}
                                    <circle
                                        cx={x} cy={y}
                                        r="20"
                                        fill={chaosMode ? "#1a0500" : (theme === 'dark' ? "#0a0a0f" : "#ffffff")}
                                        stroke={statusColor}
                                        strokeWidth={chaosMode ? 3 : 2}
                                        filter={theme === 'dark' ? "url(#glow)" : "none"}
                                        style={theme === 'light' ? {
                                            filter: 'drop-shadow(0 2px 4px rgba(0,0,0,0.15))',
                                            border: `2px solid ${statusColor}`
                                        } : {}}
                                        className={chaosMode ? "animate-pulse" : ""}
                                    />

                                    {/* Status Indicator Pulse */}
                                    {(!isUp || chaosMode) && (
                                        <circle cx={x} cy={y} r="25" fill="none" stroke={statusColor} strokeOpacity="0.5">
                                            <animate attributeName="r" from="20" to="35" dur="1s" repeatCount="indefinite" />
                                            <animate attributeName="opacity" from="1" to="0" dur="1s" repeatCount="indefinite" />
                                        </circle>
                                    )}

                                    {/* Text Labels */}
                                    <text
                                        x={x}
                                        y={y + 35}
                                        textAnchor="middle"
                                        fill={isDown || chaosMode ? "#ef4444" : (theme === 'dark' ? "#aaa" : "#333")}
                                        fontSize="10"
                                        className="font-inter font-bold"
                                        style={theme === 'light' ? { filter: 'drop-shadow(0 1px 2px rgba(255,255,255,0.8))' } : {}}
                                    >
                                        {svc.Service.replace(' Service', '')}
                                    </text>
                                    <text
                                        x={x}
                                        y={y}
                                        dy="4"
                                        textAnchor="middle"
                                        fill={theme === 'dark' || chaosMode ? "#fff" : "#1a1a1a"}
                                        fontSize="9"
                                        className="font-mono font-bold"
                                        style={theme === 'light' ? { filter: 'drop-shadow(0 1px 2px rgba(255,255,255,0.8))' } : {}}
                                    >
                                        {Math.round(svc.Cpu)}%
                                    </text>
                                </g>
                            );
                        })}
                    </svg>
                </div>
            </div>
        </div>
    );
};

export default ServiceMesh;
