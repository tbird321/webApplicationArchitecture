import React, { useState,useMemo,useEffect } from 'react';
import { Auth } from 'aws-amplify';
import AppStateContext from './AppStateContext';
import { FileProcessing } from './FileProcessing';
import { DatabaseProcessing } from './DatabaseProcessing';

//Function Provider for Data File Access
//Allows for the exposure of asynchronouse functions as hooks
export const AppStateContextProvider = ({ children, value }) => {
    const [config, setConfig] = useState(value);
    const [loggedIn, setLoggedIn] = useState(false);

    //Site Constants
    const [affiliatedSites, setAffiliatedSites] = useState(null);
    const [pageKeywords, setPageKeywords] = useState([]);
    const [pageTopics, setPageTopics] = useState([]);
    const [pageLayouts, setPageLayouts] = useState([]);
    const [currentPage, setCurrentPage] = useState({});
    const [showSpinner, setShowSpinner] = useState(false);

    const loginTimeout = 1800000 //30 minutes;

    const getLocalStoreUser = () => {
        const expirationTime = localStorage.getItem('expirationTime');
        var userData = localStorage.getItem('user');
        if (userData != null) {
            userData = JSON.parse(userData);
        }
        const curTime = new Date().getTime();
        if (!expirationTime || curTime > parseInt(expirationTime)) {
            userData = null;
        }
        return { user: userData, expTime: expirationTime };
    };

    const setLocalStoreUser = (user) => {
        const now = new Date().getTime();
        localStorage.setItem('user', JSON.stringify({ 'username': user.username, 'userEmail': user.attributes.email }));
        localStorage.setItem('expirationTime', now + loginTimeout);
    };

    const Authorization = useMemo(() => ({
        isUserLoggedIn: () => {
            const userInfo = getLocalStoreUser();
            return userInfo.user != null; // Return true if user is not null
        },
        authenticateUser: async (username, password) => {
            try {
                const user = await Auth.signIn(username, password);
                setLocalStoreUser(user);
                setLoggedIn(true);
                return user;
            } catch (err) {
                console.log(err);
            }
            return null;
        },
        logoutUser: async () => {
            try {
                await Auth.signOut();
                localStorage.removeItem('user');
                setLoggedIn(false);
            } catch (err) {
                console.log(err);
            }
        },
        getConfiguration: () => {
            return config;
        },
        getAuthenticatedUser: async () => {
            try {
                const userInfo = getLocalStoreUser();
                if (userInfo.user != null) {
                    return userInfo.user;
                } else {
                    const currentUser = await Auth.currentAuthenticatedUser();
                    setLocalStoreUser(currentUser);
                    return currentUser || null;
                }
            } catch (error) {
                return null;
            }
        },
    }), [config]); // Dependencies of the values used inside `useMemo`.
   

    const WebSiteState = useMemo(() => ({
        setShowSpinner: (bool) => {
            setShowSpinner(bool);
        },
        getShowSpinner: () => {
            return showSpinner;
        },
        webSites: () => {
            return affiliatedSites;
        },
        setWebSites: (dbWebSites) => {
            setAffiliatedSites(dbWebSites);
        },
        websiteID: () => {
            return config?.Site?.websiteId;
        },
        isLoggedIn: () => {
            return loggedIn;
        },
        setIsLoggedIn: (editMode) => {
            setLoggedIn(editMode);
        },
        getPageTopics: () => {
            return pageTopics;
        },
        setPageTopics: (topics) => {
            setPageTopics([...topics]);
        },
        getPageKeywords: () => {
            return pageKeywords;
        },
        setPageKeywords: (keywords) => {
            setPageKeywords([...keywords]);
        },
        getPageLayouts: () => {
            return pageLayouts;
        },
        setPageLayouts: (layouts) => {
            setPageLayouts([...layouts]);
        },
        getCurrentPage: () => {
            return currentPage;
        },
        setCurrentPage: (page) => {
            setCurrentPage({ ...page });
        }
    }), [affiliatedSites, currentPage, loggedIn, pageKeywords, pageLayouts, pageTopics, showSpinner, config?.Site?.websiteId]); 

    // Removed SiteFeatures from global context to keep provider lean.

    useEffect(() => {
        const initializeApplicationState = async (configInfo) => {
            // Setup default values for page layouts, keywords, and topics dropdowns
            WebSiteState.setPageKeywords(await DatabaseProcessing.getKeywords());
            WebSiteState.setPageTopics(await DatabaseProcessing.getTopics());
            WebSiteState.setPageLayouts(await DatabaseProcessing.getLayouts());
            var affiliatedWebsites = await DatabaseProcessing.getWebsites();
            WebSiteState.setWebSites(affiliatedWebsites);
            WebSiteState.setShowSpinner(false);
        };        

        if (config !== value) {
            setConfig(value);
            initializeApplicationState(value);
        }
        setLoggedIn(getLocalStoreUser().user != null);
    }, [WebSiteState, config, value]); 

    const contextValue = useMemo(() => ({
        FileProcessing,
        DatabaseProcessing,
        Authorization,
        WebSiteState
    }), [Authorization, WebSiteState]); // No dependencies, so this value is only created once.


    return (
        <AppStateContext.Provider value={contextValue}>
            {children}
        </AppStateContext.Provider>
    );
};