import { ThemeProvider } from '@mui/material/styles';
import { blueTheme } from '@tbirdcomponents/reactcomponents';
import React, { useEffect, useState } from 'react';
import { Route, BrowserRouter as Router, Routes, useLocation } from 'react-router-dom';
import './App.css';
import { AppStateContextProvider } from './hooks/appState/AppStateContextProvider';
import { ConfigProvider } from './hooks/configuration/ConfigContext';
import useConfig from './hooks/configuration/useConfig';
import SiteContainer from './sitecontent/SiteContainer';
import AdminLayout from './admin/AdminLayout';
import Dashboard from './admin/pages/Dashboard';
import PagesManager from './admin/pages/PagesManager';
import ArticlesManager from './admin/pages/ArticlesManager';
import MenuManager from './admin/pages/MenuManager';
import CollectionsManager from './admin/pages/CollectionsManager';
import ThemeManager from './admin/pages/ThemeManager';

function App() {
    function MainAppComponent() {
        const { configLoaded, config } = useConfig(); // Load the application Configuration from the config.json file.
        const [pageName, setPageName] = useState("");
        const [articleName, setArticleName] = useState(null);
        const location = useLocation();

        const handlePageNameChange = (input) => {
            setPageName(input)
        };

        //Allows the site to change the configuration based upon the url in the query string when hitting the site.
        useEffect(() => {
            const queryParams = new URLSearchParams(location.search);
            setPageName(queryParams.get('page') ?? 'Home');
            setArticleName(queryParams.get('article') ?? '');
           
        }, [location]); // When the URL changes i.e. the page changes, update the pageName and articleName.

        useEffect(() => {
            console.log("Page name changed:", pageName);
            // Perform any side effects here
        }, [pageName]);

        return (
            <ThemeProvider theme={blueTheme}>
                {/* Load the application configuration and pass it to the SiteContainer component. */}
                <ConfigProvider value={config}>
                    {/* Load the application state and pass it to the SiteContainer component. */}
                    <AppStateContextProvider value={config}>
                        {configLoaded && <SiteContainer onPageNameChange={handlePageNameChange} pageName={pageName} articleName={articleName} config={config} />}
                    </AppStateContextProvider>
                </ConfigProvider>
            </ThemeProvider>
        );
    }

    function AdminShell() {
        const { configLoaded, config } = useConfig();
        return (
            <ThemeProvider theme={blueTheme}>
                <ConfigProvider value={config}>
                    <AppStateContextProvider value={config}>
                        {configLoaded ? <AdminLayout /> : null}
                    </AppStateContextProvider>
                </ConfigProvider>
            </ThemeProvider>
        );
    }

    return (
        <div>
            <Router>
                <Routes>
                    <Route path="/" element={<MainAppComponent />} />
                    <Route path="/admin" element={<AdminShell />}>
                        <Route index element={<Dashboard />} />
                        <Route path="dashboard" element={<Dashboard />} />
                        <Route path="pages" element={<PagesManager />} />
                        <Route path="pages/:id" element={<PagesManager />} />
                        <Route path="articles" element={<ArticlesManager />} />
                        <Route path="menus" element={<MenuManager />} />
                        <Route path="collections" element={<CollectionsManager />} />
                        <Route path="themes" element={<ThemeManager />} />
                    </Route>
                </Routes>
            </Router>
        </div>
    );
}

export default App;
