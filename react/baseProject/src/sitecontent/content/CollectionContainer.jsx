import React, { useEffect, useState } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import Button from "@mui/material/Button";
import { NestedDynamicMenu } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import HtmlContentRenderer from './HtmlContentRenderer'; // Adjust the import path accordingly
import { HtmlEditor } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import MenuEditor from './MenuEditor'
import ArticleEditor from './ArticleEditor'

import EditIcon from '@mui/icons-material/Edit';
import './PageContainer.css';
function CollectionContainer({ collectionInfo, onToggleCollection, editMenu, pageModel, imageList, config, onPageChange, onSetCollectionId, collectionId }) {

    const { FileProcessing, WebSiteState, DatabaseProcessing } = useAppStateContext();
    const [editHeader, setEditHeader] = useState(false);
    //read main menu file test commit
    const [menuObject, setMenuObject] = useState(null); // State to track if config is loaded.
    const [headerHTML, setHeaderHTML] = useState(null); // State to track if config is loaded.
    const [dynamicCSSURL, setDynamicCSSURL] = useState('');
    const [articles, setArticles] = useState(null);
    const [layout, setLayout] = useState(null);
    const [searchType, setSearchType] = useState();


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
    const handleSearchType = (string) => {
        setSearchType(string);
    };
    const handleSaveHeaderHtml = async (htlmContent) => {
        const folder = 'websites/' + config.Site.siteName;
        await FileProcessing.saveFileData(folder, config.Site.headerFileName, htlmContent, 'text/html');
        setHeaderHTML(htlmContent);
    };
    const handleToggleCollection = () => {
        onToggleCollection(true);
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
        if (searchType === "page") {
            const pageInfo = await DatabaseProcessing.searchPage(criteria);
            console.log(pageInfo);
            return pageInfo;
        }
        if (searchType === "collection") {
            const collectionInfo = await DatabaseProcessing.searchCollection(criteria);
            console.log(collectionInfo);
            return collectionInfo;
        }

    };

    //Load Initial Menu data from the file
    useEffect(() => {
        const getPageInfo = async () => {
            // Early return if siteInfo is null or undefined
            WebSiteState.setShowSpinner(true);
            const rootPath = `websites/${config.Site.siteName}`;
            const headerInfo = await FileProcessing.getFileData(rootPath, config.Site.headerFileName);
            const menuInfo = await FileProcessing.getFileDataObject(rootPath, config.Site.menuConfigFileName);

            setMenuObject(menuInfo);
            setHeaderHTML(headerInfo);
            WebSiteState.setShowSpinner(false);
        }
        getPageInfo();
    }, [FileProcessing, WebSiteState, config]);

    //Load Theme Information based upon Page: TODO bind to Page
    useEffect(() => {
        // Function to set the CSS file URL
        const setCSSFileURL = () => {
            // Early return if siteInfo is null or undefined
            const rootPath = `${config.Site.appURL}public/assets/${config.Site.siteName}/themes`;
            const cssURL = `${rootPath}/${config.Site.themeFileName}`;
            // Assuming `cssURL` is the full path you need
            setDynamicCSSURL(cssURL); // Use a state to store the URL instead of the CSS content
        };

        // Call the function
        setCSSFileURL();
    }, [config]); // Dependency on config as it determines the CSS URL



    //Dynamically bind style element to head for render... 
    useEffect(() => {
        if (!dynamicCSSURL) return; // Do nothing if the URL hasn't been set

        // Create a link element
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = dynamicCSSURL;

        // Append link element to the head
        document.head.appendChild(link);

        // Cleanup the effect when the component unmounts
        return () => {
            // Directly use the created link element for removal
            if (document.head.contains(link)) {
                document.head.removeChild(link);
            }
        };
    }, [dynamicCSSURL]); // Re-run this effect if the dynamicCSSURL state changes



    //Load Page and Render - Initial page based upon settings config.initialPage TODO: adjust to use Database based upon page name and then articles... 
    useEffect(() => {
        const fetchArticles = async () => {
            WebSiteState.setShowSpinner(true);
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
            WebSiteState.setShowSpinner(false);
        };

        fetchArticles();
    }, [FileProcessing, WebSiteState, config.Site.siteName, pageModel]);

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
    };
    const handleSelectPage = (data) => {
        if (data.collectionId) {
            onSetCollectionId(data.collectionId);
        } else
        {
            onSetCollectionId(null);
        }
        onPageChange(data)
    };

    return (
        <div className='pageContents'>
            {!collectionId === null &&
                <Button onClick={handleToggleCollection}>Collection</Button>
            }
            {WebSiteState.isLoggedIn() && <div style={{ float: 'right' }}>
                <EditIcon onClick={handleEditMode} />
            </div>}
            <div className='headerContents'>
                {headerHTML && !editHeader && <HtmlContentRenderer htmlContent={headerHTML} />}
                {headerHTML && editHeader && dynamicCSSURL !== '' && <HtmlEditor cssfilePath={dynamicCSSURL} initialHtml={headerHTML} onSave={handleSaveHeaderHtml} onContentChanged={handleContentChanged} imagesUploadHandler={onImageUploaded} images={imageList}> </HtmlEditor>}
            </div>
            <div className='menuContents'>
                {menuObject && !editMenu && <NestedDynamicMenu menuItems={menuObject} onClick={handleSelectPage} />}
                {menuObject && editMenu && <MenuEditor menuItems={menuObject} dataGridColumns={columns} onSearch={handleOnSearch} onSaveChanges={handleSaveMenu} onSearchType={handleSearchType} searchType={searchType} ></MenuEditor>}
            </div>
            <div className={`articleContents ${layout}`}>
                {articles?.map((article) => (
                    <div>
                        <ArticleEditor key={article.id} cssfilePath={dynamicCSSURL} article={article} siteInfo={config} images={imageList} onImageUploaded={onImageUploaded} />
                    </div>
                ))}
            </div>
        </div>
    );
}

export default CollectionContainer;