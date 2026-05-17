import React, { useEffect, useState } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import { NestedDynamicMenu } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import HtmlContentRenderer from './HtmlContentRenderer'; // Adjust the import path accordingly
import { HtmlEditor } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import { useHomePageFeatures } from '../../hooks/appState/useSiteFeatures';
import MenuEditor from './MenuEditor'
import ArticleEditor from './ArticleEditor'
import PageGallery from './PageGallery'

import EditIcon from '@mui/icons-material/Edit';
import './PageContainer.css';
function PageContainer({ editMenu, pageModel, imageList, config, onPageChange }) {

    const { FileProcessing, WebSiteState, DatabaseProcessing } = useAppStateContext();
    const [editHeader, setEditHeader] = useState(false);
    //read main menu file test commit
    const [menuObject, setMenuObject] = useState(null); // State to track if config is loaded.
    const [headerHTML, setHeaderHTML] = useState(''); // State to track if config is loaded.
    const [dynamicCSSURL, setDynamicCSSURL] = useState('');
    const [dynamicCSSURLBase, setDynamicCSSURLBase] = useState('');
    const [articles, setArticles] = useState(null);
    const [layout, setLayout] = useState(null);
    const { shouldRenderGallery, galleryPages, showGalleryHeader, showHeaderContentInGallery } = useHomePageFeatures({ config, pageModel });
    const [availableStyleClasses, setAvailableStyleClasses] = useState([]);

    useEffect(() => {
        const processLayout = (layout) => {
            let processedStr = layout
                .replace(/\s+/g, '') // Remove all whitespace
                .replace(/,/g, '-')  // Replace commas with "-"
                .replace(/\//g, '_') // Replace "/" with "_"
                .replace(/Grid/gi, '') // Remove the word "Grid", case-insensitive
                .toLowerCase();      // Convert to lowercase

            return `layout-${processedStr}`; // Prefix with "layout-"
        };
        if (pageModel && pageModel.layout) {
            let newLayout = processLayout(pageModel.layout);
            setLayout(newLayout)
        }
    }, [pageModel]);

    const columns = [
        { field: "name", Header: "Name", Type: "Text" },
        { field: "description", Header: "Description", Type: "Text" },
    ];

    const handleSaveHeaderHtml = async (htlmContent) => {
        const folder = 'websites/' + config.Site.siteName;
        await FileProcessing.saveFileData(folder, config.Site.headerFileName, htlmContent, 'text/html');
        setHeaderHTML(htlmContent);
    };

    const handleSaveMenu = async (menuItems) => {
        const rootPath = 'websites/' + config.Site.siteName;
        await FileProcessing.saveFileData(rootPath, config.Site.menuConfigFileName, JSON.stringify(menuItems));
        setMenuObject(menuItems);
    };


    const handleContentChanged = (htlmContent) => {
        setHeaderHTML(htlmContent);
    };

    const handleOnSearch = async (criteria) => {
        console.log(criteria);
        criteria.websiteId = WebSiteState.websiteID();
        const pageInfo = await DatabaseProcessing.searchPage(criteria);
        console.log(pageInfo);
        return pageInfo;
    };

    //Load Initial Menu data from the file
    useEffect(() => {
        const transformData = (data) => {
            const map = {};
            const result = [];

            if (Array.isArray(data)) {
                // Create a map of all items
                data.forEach(item => {
                    const itemTitle = item?.itemTitle ?? item?.pageName ?? item?.text ?? '';
                    map[item.id] = { ...item, itemTitle, menuItems: [] };
                });

                // Build the tree structure
                data.forEach(item => {
                    if (item.parent === 0) {
                        result.push(map[item.id]);
                    } else {
                        if (map[item.parent]) {
                            map[item.parent].menuItems.push(map[item.id]);
                        } else {
                            // Handle the case where the parent is not found
                            result.push(map[item.id]);
                        }
                    }
                });
            }

            return result;
        };
        const getPageInfo = async () => {
            // Early return if siteInfo is null or undefined
            //WebSiteState.setShowSpinner(true);
            const rootPath = `websites/${config.Site.siteName}`;
            var headerInfo = '';
            var menuInfo = [];
            if (await FileProcessing.fileExists(rootPath, config.Site.headerFileName) === true) {
                headerInfo = await FileProcessing.getFileData(rootPath, config.Site.headerFileName);
            }
            if (await FileProcessing.fileExists(rootPath, config.Site.menuConfigFileName) === true) {
                menuInfo = await FileProcessing.getFileDataObject(rootPath, config.Site.menuConfigFileName);
                const tranData = transformData(menuInfo);
                setMenuObject(tranData);
            }
            setHeaderHTML(headerInfo);
            //WebSiteState.setShowSpinner(false);
        }
        getPageInfo();
    }, [FileProcessing, WebSiteState, config]);

    // Debug logging for menu items when enabled
    useEffect(() => {
        if (!config?.Site?.appLoggingEnabled) return;
        if (!menuObject) return;
        const flatten = (items) => {
            const acc = [];
            const walk = (itms, depth = 0) => {
                (itms || []).forEach(it => {
                    acc.push({ id: it.id, text: it.text, pageName: it.pageName, itemTitle: it.itemTitle, depth });
                    if (Array.isArray(it.menuItems) && it.menuItems.length > 0) {
                        walk(it.menuItems, depth + 1);
                    }
                });
            };
        walk(items);
            return acc;
        };
        // eslint-disable-next-line no-console
        console.log('PageContainer menuObject:', menuObject);
        // eslint-disable-next-line no-console
        console.log('PageContainer menu (flat):', flatten(menuObject));
    }, [menuObject, config?.Site?.appLoggingEnabled]);

    //Load Theme Information based upon Page: cascade base theme then page style
    useEffect(() => {
        // Function to set the CSS file URL
        const setCSSFileURL = () => {
            // Early return if siteInfo is null or undefined
            if (!config?.Site?.appURL || !config?.Site?.siteName) return;
            const trimmedBase = String(config.Site.appURL).replace(/\/+$/, '');
            const rootPath = `${trimmedBase}/public/assets/${config.Site.siteName}/themes`;
            const cacheBust = `v=${Date.now()}`;

            const baseThemeNameRaw = String(config?.Site?.themeFileName || '').trim();
            const baseThemeName = baseThemeNameRaw
                ? (baseThemeNameRaw.endsWith('.css') ? baseThemeNameRaw : `${baseThemeNameRaw}.css`)
                : '';
            const pageThemeNameRaw = String(pageModel?.style || '').trim();
            const pageThemeName = pageThemeNameRaw
                ? (pageThemeNameRaw.endsWith('.css') ? pageThemeNameRaw : `${pageThemeNameRaw}.css`)
                : '';

            const baseUrl = baseThemeName ? `${rootPath}/${baseThemeName}?${cacheBust}` : '';
            const pageUrl = pageThemeName && pageThemeName.toLowerCase() !== baseThemeName.toLowerCase()
                ? `${rootPath}/${pageThemeName}?${cacheBust}`
                : '';

            setDynamicCSSURLBase(baseUrl);
            // For editors, prefer page-specific CSS if present, else base
            setDynamicCSSURL(pageUrl || baseUrl);
        };

        // Call the function
        setCSSFileURL();
    }, [config, pageModel?.style]); // Update when config or page style changes



    //Dynamically bind style elements for cascading: base then page-specific
    useEffect(() => {
        if (!dynamicCSSURLBase && !dynamicCSSURL) return;

        const createdLinks = [];
        const appendLink = (href) => {
            if (!href) return;
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = href;
            link.onload = () => {
                if (window.__APP_LOGGING_ENABLED__) {
                    // eslint-disable-next-line no-console
                    console.log('Theme CSS loaded:', href);
                }
            };
            link.onerror = (e) => {
                // eslint-disable-next-line no-console
                console.error('Theme CSS failed to load:', href, e);
            };
            document.head.appendChild(link);
            createdLinks.push(link);
        };

        // Remove any pre-existing theme links for this site to enforce ordering
        try {
            const siteToken = `/public/assets/${config?.Site?.siteName}/themes/`;
            document.querySelectorAll('link[rel="stylesheet"]').forEach(l => {
                const href = l.getAttribute('href') || '';
                if (href.includes(siteToken)) {
                    if (document.head.contains(l)) document.head.removeChild(l);
                }
            });
        } catch (e) {
            // eslint-disable-next-line no-console
            console.warn('Theme cleanup skipped', e);
        }

        // Ensure base loads first, then page overrides
        appendLink(dynamicCSSURLBase);
        if (dynamicCSSURL && dynamicCSSURL !== dynamicCSSURLBase) {
            appendLink(dynamicCSSURL);
        }

        return () => {
            createdLinks.forEach(l => {
                if (document.head.contains(l)) document.head.removeChild(l);
            });
        };
    }, [dynamicCSSURLBase, dynamicCSSURL, config?.Site?.siteName]);

    // Extract class names from loaded theme CSS for HtmlEditor style formats
    useEffect(() => {
        let cancelled = false;
        const extractClassesFromCssText = (cssText) => {
            const cls = new Set();
            if (!cssText) return [];
            cssText.replace(/(^|[^a-zA-Z0-9_-])\.([a-zA-Z_-][\w-]*)/g, (m, _p, name) => {
                cls.add(name);
                return m;
            });
            return Array.from(cls).sort((a, b) => a.localeCompare(b));
        };
        const fetchCss = async (href) => {
            try {
                if (!href) return '';
                const res = await fetch(href, { cache: 'no-store' });
                return await res.text();
            } catch {
                return '';
            }
        };
        const run = async () => {
            const texts = await Promise.all([
                fetchCss(dynamicCSSURLBase),
                dynamicCSSURL !== dynamicCSSURLBase ? fetchCss(dynamicCSSURL) : Promise.resolve('')
            ]);
            const all = extractClassesFromCssText(texts.join('\n'));
            if (!cancelled) setAvailableStyleClasses(all);
        };
        if (dynamicCSSURLBase || dynamicCSSURL) run();
        return () => { cancelled = true; };
    }, [dynamicCSSURLBase, dynamicCSSURL]);



    //Load Page and Render - Initial page based upon settings config.initialPage TODO: adjust to use Database based upon page name and then articles... 
    useEffect(() => {
        const fetchArticles = async () => {
            //WebSiteState.setShowSpinner(true);
            try {
                const rootPath = `websites/${config.Site.siteName}`;
                if (pageModel) {
                    //load all articles from the page data...
                    const articlesWithHTML = await Promise.all(pageModel.articles.map(async (item) => {
                        try {
                            if (item.articlePath) {
                                const articleHTML = await FileProcessing.getFileData(`${rootPath}/articles`, item.articlePath);
                                return { ...item, articleHTML }; // Return the updated item.
                            }
                        } catch (articleError) {
                            console.error("Failed to fetch article HTML:", articleError);
                            return { ...item, articleHTML: '' }; // Return item with empty HTML in case of error
                        }
                    }));
                    setArticles(articlesWithHTML);
                }
            } catch (error) {
                console.error("Failed to fetch articles:", error);
                // Handle error state as needed
            }
            //WebSiteState.setShowSpinner(false);
        };

        fetchArticles();
    }, [FileProcessing, WebSiteState, config.Site.siteName, pageModel]);

    // No local effect; gallery logic handled by useHomePageFeatures

    const onImageUploaded = async (blobInfo, success, failure) => {
        const folder = 'assets/' + config.Site.siteName + '/images';
        const fileData = blobInfo.blob();

        //saveFileData: async (folder, fileName, fileData, contentType)
        await FileProcessing.saveFileData(folder, blobInfo.filename(), fileData, fileData.type);
        var fullFilename = config.Site.appURL + 'public/' + folder + '/' + blobInfo.filename();
        success(fullFilename);
        return fullFilename;
    };
    const handleEditMode = () => {
        setEditHeader(!editHeader);
        if (!editHeader) {
            if (headerHTML === '') {
                setHeaderHTML('<div>Header PlaceHolder</div>');
            }
        }
    };
    const handleSelectPage = (data) => {

        onPageChange(data)
    };

    const headerClick = () => {
        onPageChange(null);
    };

    return (
        <div className='pageContents'>
            {WebSiteState.isLoggedIn() && <div style={{ float: 'right' }}>
                <EditIcon onClick={handleEditMode} />
            </div>}
            <div className='headerContents' onClick={headerClick}>
                {headerHTML && !editHeader && <HtmlContentRenderer htmlContent={headerHTML} />}
                {headerHTML && editHeader && dynamicCSSURL !== '' && <div style={{ minHeight: '150px' }}>
                    <HtmlEditor cssfilePath={dynamicCSSURL} initialHtml={headerHTML} onSave={handleSaveHeaderHtml} onContentChanged={handleContentChanged} imagesUploadHandler={onImageUploaded} images={imageList}> </HtmlEditor>
                    &nbsp;
                </div>}
            </div>
            <div className='menuContents'>
                {menuObject && !editMenu && <NestedDynamicMenu menuItems={menuObject} onClick={handleSelectPage} showTitles={false} />}
                {config?.Site?.appLoggingEnabled && (
                    <script dangerouslySetInnerHTML={{ __html: 'window.__APP_LOGGING_ENABLED__=true;' }} />
                )}
                {menuObject && editMenu && (
                    <MenuEditor
                        menuItems={(function normalize(items){
                            const fix = (it)=> ({
                                ...it,
                                itemTitle: it?.itemTitle ?? it?.pageName ?? it?.text ?? '',
                                menuItems: Array.isArray(it?.menuItems) ? it.menuItems.map(fix) : []
                            });
                            return (items||[]).map(fix);
                        })(menuObject)}
                        dataGridColumns={columns}
                        onSearch={handleOnSearch}
                        onSaveChanges={handleSaveMenu}
                    />
                )}
            </div>
            <div className={`articleContents ${layout}`}>
                {articles?.map((article, index) => (
                    <div key={`article-wrap-${article.id || index}`}>
                        <ArticleEditor key={article.id} cssfilePath={dynamicCSSURL} styleClasses={availableStyleClasses} article={article} siteInfo={config} images={imageList} onImageUploaded={onImageUploaded} />
                        {index === 0 
                            && shouldRenderGallery && (
                            <PageGallery
                                pages={Array.isArray(galleryPages) ? galleryPages : []}
                                config={config}
                                imageList={imageList}
                                onPageChange={onPageChange}
                                showHeader={showGalleryHeader}
                                showHeaderContent={showHeaderContentInGallery}
                            />
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}

export default PageContainer;