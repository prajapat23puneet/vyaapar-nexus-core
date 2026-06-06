import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import App from './App.tsx';
import { store } from './store';
import './index.css';

// FIX-6: Apply dark class to <html> — PRD mandates "dark theme first".
// uiSlice initialState has theme: 'dark'. The .dark CSS class in index.css
// activates all dark-mode custom properties and shadcn sidebar dark tokens.
document.documentElement.classList.add('dark');

import { HelmetProvider } from 'react-helmet-async';

const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <HelmetProvider>
      <Provider store={store}>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </QueryClientProvider>
      </Provider>
    </HelmetProvider>
  </React.StrictMode>,
);
