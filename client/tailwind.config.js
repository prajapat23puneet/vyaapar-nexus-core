/** @type {import('tailwindcss').Config} */
export default {
    content: [
        "./index.html",
        "./src/**/*.{js,ts,jsx,tsx}",
    ],
    darkMode: 'class', // Enable class-based dark mode
    theme: {
        extend: {
            colors: {
                'vedic-gold': '#FFD700',
                'cosmic-black': '#0a0a0f',
                'neon-teal': '#00f3ff',
                'deep-maroon': '#800000',
                'cyber-gray': '#1a1a2e',
                'paper-white': '#f5f5f5',
            },
            fontFamily: {
                orbitron: ['Orbitron', 'sans-serif'],
                inter: ['Inter', 'sans-serif'],
                rajdhani: ['Rajdhani', 'sans-serif'],
            },
            backgroundImage: {
                'vedic-gradient': 'linear-gradient(135deg, #0a0a0f 0%, #1a1a2e 100%)',
            },
            animation: {
                'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
                'spin-slow': 'spin 10s linear infinite',
            },
        },
    },
    plugins: [],
}
