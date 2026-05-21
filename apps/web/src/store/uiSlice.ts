import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export interface UiState {
  orderDetailsDrawerOpen: boolean;
  lastActionError: string | null;
  theme: 'light' | 'dark' | 'system';
}

const initialState: UiState = {
  orderDetailsDrawerOpen: false,
  lastActionError: null,
  theme: 'dark', // Enforcing dark theme first
};

export const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    setOrderDetailsDrawerOpen: (state, action: PayloadAction<boolean>) => {
      state.orderDetailsDrawerOpen = action.payload;
    },
    setLastActionError: (state, action: PayloadAction<string | null>) => {
      state.lastActionError = action.payload;
    },
    setTheme: (state, action: PayloadAction<'light' | 'dark' | 'system'>) => {
      state.theme = action.payload;
    },
  },
});

export const { setOrderDetailsDrawerOpen, setLastActionError, setTheme } = uiSlice.actions;
export default uiSlice.reducer;
