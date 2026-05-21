import React, { Component, ErrorInfo, ReactNode } from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';

interface Props {
  children: ReactNode;
  fallbackTitle?: string;
}

interface State {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = {
    hasError: false,
    error: null,
    errorInfo: null
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error, errorInfo: null };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Uncaught error inside ErrorBoundary:', error, errorInfo);
    this.setState({ errorInfo });
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null, errorInfo: null });
    window.location.reload();
  };

  public render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center p-8 rounded-xl border border-red-500/20 bg-red-500/5 text-center max-w-2xl mx-auto my-6 space-y-4 shadow-lg">
          <div className="p-3 bg-red-500/10 rounded-full text-red-500 animate-bounce">
            <AlertCircle className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-bold text-red-500">
            {this.props.fallbackTitle || 'A rendering error occurred'}
          </h2>
          <p className="text-sm text-[var(--text)] max-w-md">
            The component crashed during rendering. Below is the error description:
          </p>
          
          <div className="w-full bg-[var(--code-bg)] text-[var(--text-h)] p-4 rounded-lg font-mono text-xs text-left overflow-x-auto border border-[var(--border)] max-h-48 whitespace-pre-wrap">
            <span className="font-bold text-red-400">{this.state.error?.name}:</span> {this.state.error?.message}
            {this.state.error?.stack && (
              <div className="mt-2 text-[var(--text)]/70 text-[10px]">
                {this.state.error.stack}
              </div>
            )}
          </div>

          <div className="flex gap-4">
            <button
              onClick={() => this.setState({ hasError: false, error: null, errorInfo: null })}
              className="px-4 py-2 text-sm font-medium rounded-md border border-[var(--border)] bg-[var(--social-bg)] text-[var(--text-h)] hover:bg-[var(--border)] transition-colors active:scale-95"
            >
              Clear Error State
            </button>
            <button
              onClick={this.handleReset}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md bg-red-500 text-white hover:bg-red-600 transition-colors active:scale-95"
            >
              <RefreshCw className="h-4 w-4" />
              Reload Page
            </button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
