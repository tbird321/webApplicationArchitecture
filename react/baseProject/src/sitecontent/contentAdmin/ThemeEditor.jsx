import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import ThemeGrid from "../content/ThemeGrid";
function ThemeEditor({ config }) {
    const [themesList, setThemesList] = useState([]);
    const { FileProcessing, WebSiteState, DatabaseProcessing } = useAppStateContext(); 

    useEffect(() => {
        const fetchThemeNames = async () => {
            try {
                const newThemesList = await FileProcessing.getThemeNames(config.Site.siteName);
                setThemesList(newThemesList);
            } catch (error) {
                console.error('Error fetching theme names:', error);
            }
        };
        fetchThemeNames();
    }, [FileProcessing, config.Site.siteName]);


    return (
        <div style={{ minWidth: '900px', minHeight: '600px' }}>
            <ThemeGrid themesList={themesList} ></ThemeGrid>
        </div>

    );
}

export default ThemeEditor;