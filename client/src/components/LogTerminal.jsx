import React, { useEffect, useRef, useState } from 'react';
import { clsx } from 'clsx';

const LogTerminal = ({ logs, theme = 'dark' }) => {
    const logContainerRef = useRef(null);
    const [isUserScrolling, setIsUserScrolling] = useState(false);
    const [autoScroll, setAutoScroll] = useState(true);

    // Detect if user is manually scrolling
    const handleScroll = (e) => {
        const element = e.target;
        // logic: if scrolled up more than 10px from bottom, consider it "user scrolling"
        const isAtBottom = Math.abs(element.scrollHeight - element.scrollTop - element.clientHeight) < 10;

        if (!isAtBottom) {
            setAutoScroll(false);
        } else {
            setAutoScroll(true);
        }
    };

    // Auto-scroll logic
    useEffect(() => {
        if (autoScroll && logContainerRef.current) {
            logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight;
        }
    }, [logs, autoScroll]);

    return (
        <div className="glass-panel p-4 rounded-xl flex flex-col h-full overflow-hidden">
            <div className={clsx(
                "flex justify-between items-center mb-2 border-b pb-2",
                theme === 'dark' ? "border-white/10" : "border-gray-200"
            )}>
                <h3 className={clsx(
                    "font-mono text-xs uppercase flex items-center gap-2",
                    theme === 'dark' ? "text-neon-teal" : "text-blue-600"
                )}>
                    <span className={clsx(
                        "w-2 h-2 rounded-full animate-pulse",
                        theme === 'dark' ? "bg-neon-teal" : "bg-blue-600"
                    )} />
                    System Logs
                </h3>

                <div className="flex gap-3 items-center">
                    <span className={clsx(
                        "text-xs",
                        theme === 'dark' ? "text-gray-500" : "text-gray-600"
                    )}>{logs.length} entries</span>

                    {/* Show indicator when auto-scroll is paused */}
                    {!autoScroll && (
                        <button
                            onClick={() => setAutoScroll(true)}
                            className={clsx(
                                "text-[10px] px-2 py-0.5 rounded uppercase tracking-wider font-bold animate-pulse",
                                theme === 'dark'
                                    ? "bg-yellow-500/20 text-yellow-400 hover:bg-yellow-500/30"
                                    : "bg-yellow-100 text-yellow-700 hover:bg-yellow-200"
                            )}
                        >
                            ⬇ Resume
                        </button>
                    )}
                </div>
            </div>

            <div
                ref={logContainerRef}
                onScroll={handleScroll}
                className="flex-1 overflow-y-auto font-mono text-xs space-y-1 pr-2 custom-scrollbar overscroll-contain"
                style={{
                    overscrollBehavior: 'contain', // Critical for preventing page scroll
                }}
            >
                {logs.length === 0 && (
                    <div className={clsx(
                        "italic",
                        theme === 'dark' ? "text-gray-600" : "text-gray-500"
                    )}>Waiting for signals...</div>
                )}
                {logs.map((log, i) => (
                    <div key={i} className="flex gap-2 animate-in fade-in slide-in-from-bottom-1 duration-300 items-start">
                        <span className={clsx(
                            "whitespace-nowrap",
                            theme === 'dark' ? "text-gray-500" : "text-gray-600"
                        )}>[{log.Timestamp}]</span>
                        <span className={clsx(
                            "font-bold w-12 inline-block",
                            theme === 'dark' && log.Level === 'INFO' && "text-blue-400",
                            theme === 'dark' && log.Level === 'WARN' && "text-yellow-400",
                            theme === 'dark' && (log.Level === 'ERROR' || log.Level === 'CRITICAL') && "text-red-500",
                            theme === 'light' && log.Level === 'INFO' && "log-info",
                            theme === 'light' && log.Level === 'WARN' && "log-warn",
                            theme === 'light' && log.Level === 'ERROR' && "log-error",
                            theme === 'light' && log.Level === 'CRITICAL' && "log-critical"
                        )}>{log.Level}</span>
                        <span className={clsx(
                            theme === 'dark' ? "text-gray-300" : "text-gray-800"
                        )}>{log.Message}</span>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default LogTerminal;
