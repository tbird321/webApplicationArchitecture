import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useAppStateContext } from '../hooks/appState/useAppStateContext';
import PageContainer from './content/PageContainer';
import UserStatus from './UserStatus';
import { Spinner } from '@tbirdcomponents/reactcomponents';
import HtmlContentRenderer from './content/HtmlContentRenderer';
import ReactGA from 'react-ga4';

function SiteContainer({ onPageNameChange, pageName, articleName, config }) {
    const { Authorization, WebSiteState, FileProcessing, DatabaseProcessing } = useAppStateContext();
    const [pageType, setPageType] = useState(null);
    const [loggedInUser, setLoggedInUser] = useState(null);
    const [editMenu, setEditMenu] = useState(false);
    const [pageModel, setPageModel] = useState(null);
    const [images, setImages] = useState([]);
    const [initialHtml, setInitialHtml] = useState('<div></div>');
    const [analyticsInit, setAnalyticsInit] = useState(false);
    useEffect(() => {
        const fetchInitialHtml = async () => {
            try {
                const response = await fetch('/initialRender.html');
                setInitialHtml(await response.text());
                if (!analyticsInit) {
                    ReactGA.initialize(config?.Site?.analyticsTag);
                    setAnalyticsInit(true);
                }
                ReactGA.send({
                    hitType: "pageview",
                    page: "Home",
                    title: "Home",
                });
            } catch (error) {
                console.error("Error loading initial HTML:", error);
                setInitialHtml('<div>Could not Load Content</div>');
            }
        };
        fetchInitialHtml();
    }, []);

    useEffect(() => {
        const fetchPageModel = async () => {
            if (pageName) {
                try {
                    const tempPageModel = await DatabaseProcessing.getPageByName(pageName, config?.Site?.websiteId);
                    setPageModel({
                        ...tempPageModel,
                        articles: Array.isArray(tempPageModel.articles) ? tempPageModel.articles : [],
                        keywords: Array.isArray(tempPageModel.keywords) ? tempPageModel.keywords : [],
                        topics: Array.isArray(tempPageModel.topics) ? tempPageModel.topics : [],
                        layouts: Array.isArray(tempPageModel.layouts) ? tempPageModel.layouts : []
                    });
                } catch (error) {
                    if (error.response?.data === 'PageID not found') {
                        console.log('Invalid page name');
                    } else {
                        console.error("Error fetching page model:", error);
                    }
                }
            }
        };
        fetchPageModel();
    }, [DatabaseProcessing, config?.Site?.websiteId, pageName]);

    useEffect(() => {
        const fetchPageInfo = async () => {
            try {
                const loggedInUser = await Authorization.getAuthenticatedUser();
                setLoggedInUser(loggedInUser);
                setPageType(config.Site.siteType);
            } catch (error) {
                console.error("Error fetching page info:", error);
            }
        };
        fetchPageInfo();
    }, [Authorization, config.Site.siteType]);

    useEffect(() => {
        const fetchImages = async () => {
            try {
                const rootPath = `assets/${config.Site.siteName}/images`;
                const fileList = await FileProcessing.getFileList(rootPath);
                const imageList = fileList.results.map(item => ({
                    url: `${config.Site.appURL}public/${item.key}`,
                    alt: ''  // Consider adding meaningful alt text
                }));
                setImages(imageList);
            } catch (error) {
                console.error("Error fetching images:", error);
            }
        };
        fetchImages();
    }, [FileProcessing, config.Site.appURL, config.Site.siteName]);

    const handleLogin = useCallback((loggedInUser) => {
        setLoggedInUser(loggedInUser);
    }, []);

    const handleLogout = useCallback(() => {
        setEditMenu(false);
    }, []);

    const handlePageChange = useCallback(async (page) => {
        try {
            if (page == null) {
                onPageNameChange('Home');
                if (!analyticsInit) {
                    ReactGA.initialize(config?.Site?.analyticsTag);
                    setAnalyticsInit(true);
                }
                ReactGA.send({
                    hitType: "pageview",
                    page: "Home",
                    title: "Home",
                });
            } else if (page.pageId) {
                const tempPageModel = await DatabaseProcessing.getPageById(page.pageId, config?.Site?.websiteId);
                setPageModel(tempPageModel);
                onPageNameChange(tempPageModel.name);
                if (!analyticsInit) {
                    ReactGA.initialize(config?.Site?.analyticsTag);
                    setAnalyticsInit(true);
                }
                ReactGA.send({
                    hitType: "pageview",
                    page: tempPageModel.name,
                    title: tempPageModel.name,
                });
            } else {
                if (!analyticsInit) {
                    ReactGA.initialize(config?.Site?.analyticsTag);
                    setAnalyticsInit(true);
                }
            }
        } catch (error) {
            if (error.response?.data === 'PageID not found') {
                console.log('Invalid page name');
            } else {
                console.error("Error fetching page model:", error);
            }
        }
    }, [DatabaseProcessing, analyticsInit, config?.Site?.analyticsTag, config?.Site?.websiteId, onPageNameChange]);

    const renderPageContent = useMemo(() => {
        if (pageType === 'BasicSite') {
            if (pageModel == null) {
                return <HtmlContentRenderer htmlContent={initialHtml} />;
            }
            return (
                <PageContainer
                    editMenu={editMenu}
                    pageModel={pageModel}
                    imageList={images}
                    config={config}
                    onPageChange={handlePageChange}
                />
            );
        }
        return <HtmlContentRenderer htmlContent={initialHtml} />;
    }, [pageType, pageModel, initialHtml, editMenu, images, config, handlePageChange]);

    return (
        <div className='bodyStyle'>
            <div style={{ float: 'right' }}>
                <UserStatus
                    user={loggedInUser}
                    onLoggedIn={handleLogin}
                    onLoggedOut={handleLogout}
                />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '16px' }}>
                <Spinner showSpinner={WebSiteState.getShowSpinner()} />
            </div>
            <div>{renderPageContent}</div>
        </div>
    );
}

export default SiteContainer;
