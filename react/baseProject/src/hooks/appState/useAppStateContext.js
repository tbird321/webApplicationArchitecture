import { useContext } from 'react';
import AppStateContext from './AppStateContext';

export const useAppStateContext = () => {
    const context = useContext(AppStateContext);

    if (!context) {
        throw new Error('useAsyncFunctions must be used within AsyncFunctionsProvider');
    }

    return context;
};