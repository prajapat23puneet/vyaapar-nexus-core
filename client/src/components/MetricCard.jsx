import React, { useMemo } from 'react';
import { clsx } from 'clsx';
import { motion } from 'framer-motion';
import { AlertTriangle } from 'lucide-react';

const MetricCard = ({ 
    title, 
    value, 
    icon: Icon, 
    trend, 
    history = [],
    color = 'gold', 
    theme = 'dark',
    metricType = 'orders' // 'orders', 'sagas', 'deadLetters'
}) => {
    // Format value based on metric type
    const formattedValue = useMemo(() => {
        if (metricType === 'orders') {
            return typeof value === 'number' ? value.toFixed(1) : value;
        }
        return Math.floor(value || 0);
    }, [value, metricType]);

    // Determine trend direction and color
    const trendDirection = trend?.direction || 'stable';
    const trendPercentage = trend?.percentage || 0;
    
    // Color logic based on metric type
    const getTrendColor = () => {
        if (metricType === 'deadLetters') {
            // For dead letters: red if > 0, green if 0
            if (value > 0) return { text: 'text-red-400', bg: 'bg-red-500/20', border: 'border-red-500/20' };
            return { text: 'text-green-400', bg: 'bg-green-500/20', border: 'border-green-500/20' };
        }
        
        if (metricType === 'sagas') {
            // Orange/yellow for sagas
            if (trendDirection === 'up') return { text: 'text-orange-400', bg: 'bg-orange-500/20', border: 'border-orange-500/20' };
            if (trendDirection === 'down') return { text: 'text-yellow-500', bg: 'bg-yellow-500/20', border: 'border-yellow-500/20' };
            return { text: 'text-gray-400', bg: 'bg-gray-500/20', border: 'border-gray-500/20' };
        }
        
        // For orders: green up, red down, gray stable
        if (trendDirection === 'up') return { text: 'text-green-400', bg: 'bg-green-500/20', border: 'border-green-500/20' };
        if (trendDirection === 'down') return { text: 'text-red-400', bg: 'bg-red-500/20', border: 'border-red-500/20' };
        return { text: 'text-gray-400', bg: 'bg-gray-500/20', border: 'border-gray-500/20' };
    };

    const trendColors = getTrendColor();

    // Get trend icon
    const getTrendIcon = () => {
        if (trendDirection === 'up') return '↑';
        if (trendDirection === 'down') return '↓';
        return '→';
    };

    // Generate sparkline data
    const sparklineData = useMemo(() => {
        if (!history || history.length < 2) return [];
        const max = Math.max(...history, 1);
        const min = Math.min(...history, 0);
        const range = max - min || 1;
        
        return history.map((val, idx) => ({
            x: (idx / (history.length - 1 || 1)) * 100,
            y: 100 - ((val - min) / range) * 100
        }));
    }, [history]);

    // Get value color based on metric type
    const getValueColor = () => {
        if (metricType === 'deadLetters') {
            return value > 0 ? 'text-red-500' : 'text-green-500';
        }
        if (metricType === 'sagas') {
            return 'text-orange-400';
        }
        return color === 'gold' ? 'text-vedic-gold' : color === 'teal' ? 'text-neon-teal' : 'text-deep-maroon';
    };

    return (
        <div className={clsx(
            "glass-panel rounded-xl relative overflow-hidden",
            "transition-all duration-300 group",
            "min-h-[220px]",
            "flex flex-col",
            "p-6",
            theme === 'light' && "shadow-md hover:shadow-lg",
            // Gradient Border Effect
            theme === 'dark' && "before:absolute before:inset-0 before:p-[1px] before:rounded-xl before:bg-gradient-to-br before:from-white/20 before:to-transparent before:content-[''] before:pointer-events-none",
            theme === 'dark' && color === 'gold' && "hover:before:from-vedic-gold/50",
            theme === 'dark' && color === 'teal' && "hover:before:from-neon-teal/50",
            theme === 'dark' && color === 'maroon' && "hover:before:from-deep-maroon/50"
        )}>
            {/* Decorative Line */}
            <div className={clsx(
                "absolute bottom-0 left-0 h-1 w-full bg-gradient-to-r from-transparent to-transparent opacity-50 z-0",
                color === 'gold' && "via-vedic-gold/50",
                color === 'teal' && "via-neon-teal/50"
            )} />

            {/* Background Pulse */}
            <div className={clsx(
                "absolute -right-10 -bottom-10 w-32 h-32 rounded-full blur-3xl opacity-5 group-hover:opacity-20 transition-opacity duration-500 pointer-events-none z-0",
                color === 'gold' && "bg-vedic-gold",
                color === 'teal' && "bg-neon-teal",
                color === 'maroon' && "bg-deep-maroon"
            )} />
            
            {/* Content Container */}
            <div className="relative z-10 flex flex-col flex-1">
            <div className="flex justify-between items-start mb-4 flex-shrink-0">
                <h3 className={clsx(
                    "font-orbitron text-sm tracking-wider uppercase",
                    theme === 'dark' ? "text-gray-400" : "text-gray-600"
                )}>{title}</h3>
                <div className="flex items-center gap-2">
                    {metricType === 'deadLetters' && value > 0 && (
                        <AlertTriangle className={clsx(
                            "w-4 h-4",
                            theme === 'dark' ? "text-red-400" : "text-red-600"
                        )} />
                    )}
                    {Icon && (
                        <Icon className={clsx(
                            "w-5 h-5 transition-colors",
                            theme === 'dark' 
                                ? "text-gray-500 group-hover:text-white"
                                : "text-gray-400 group-hover:text-gray-700"
                        )} />
                    )}
                </div>
            </div>

            {/* Large Number with Animation */}
            <div className="flex-1 flex items-center justify-start my-4 py-2">
                <motion.div
                    key={formattedValue}
                    initial={{ opacity: 0.5, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ duration: 0.3, ease: "easeOut" }}
                    className={clsx(
                        "text-4xl sm:text-5xl font-rajdhani font-bold drop-shadow-lg transition-colors duration-300 leading-tight",
                        getValueColor()
                    )}
                    style={{ lineHeight: '1.2' }}
                >
                    {formattedValue}
                </motion.div>
            </div>

            {/* Trend Indicator and Percentage */}
            <div className="flex items-center gap-2 mb-3 flex-shrink-0">
                <div className={clsx(
                    "flex items-center gap-1 text-sm font-semibold px-2 py-1 rounded-md backdrop-blur-sm border",
                    theme === 'dark' ? trendColors.bg : trendColors.bg.replace('/20', '/30'),
                    theme === 'dark' ? trendColors.border : trendColors.border.replace('/20', '/30'),
                    theme === 'dark' ? trendColors.text : trendColors.text.replace('400', '700')
                )}>
                    <span className="text-xs">{getTrendIcon()}</span>
                    <span className="text-xs">
                        {trendPercentage > 0 ? '+' : ''}{trendPercentage.toFixed(1)}%
                    </span>
                </div>
            </div>

            {/* Sparkline Chart */}
            {sparklineData.length > 1 && (
                <div className="h-16 w-full overflow-hidden mt-auto flex-shrink-0">
                    <svg className="w-full h-full" viewBox="0 0 100 100" preserveAspectRatio="none">
                        <defs>
                            <linearGradient id={`gradient-${metricType}`} x1="0%" y1="0%" x2="0%" y2="100%">
                                <stop offset="0%" stopColor={metricType === 'deadLetters' 
                                    ? (value > 0 ? 'rgba(239, 68, 68, 0.3)' : 'rgba(34, 197, 94, 0.3)')
                                    : metricType === 'sagas'
                                    ? 'rgba(251, 146, 60, 0.3)'
                                    : 'rgba(217, 119, 6, 0.3)'} />
                                <stop offset="100%" stopColor={metricType === 'deadLetters'
                                    ? (value > 0 ? 'rgba(239, 68, 68, 0)' : 'rgba(34, 197, 94, 0)')
                                    : metricType === 'sagas'
                                    ? 'rgba(251, 146, 60, 0)'
                                    : 'rgba(217, 119, 6, 0)'} />
                            </linearGradient>
                        </defs>
                        <motion.path
                            d={`M ${sparklineData.map((p, i) => `${p.x},${p.y}`).join(' L ')}`}
                            fill="none"
                            stroke={metricType === 'deadLetters'
                                ? (value > 0 ? '#ef4444' : '#22c55e')
                                : metricType === 'sagas'
                                ? '#fb923c'
                                : '#d97706'}
                            strokeWidth="2"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            initial={{ pathLength: 0, opacity: 0 }}
                            animate={{ pathLength: 1, opacity: 1 }}
                            transition={{ duration: 0.5, ease: "easeInOut" }}
                        />
                        <motion.path
                            d={`M ${sparklineData.map((p, i) => `${p.x},${p.y}`).join(' L ')} L 100,100 L 0,100 Z`}
                            fill={`url(#gradient-${metricType})`}
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ duration: 0.5, delay: 0.2 }}
                        />
                    </svg>
                </div>
            )}
            </div>
        </div>
    );
};

export default MetricCard;
