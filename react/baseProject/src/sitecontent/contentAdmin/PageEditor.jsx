import React, { useState, useEffect, useMemo } from "react";
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import CreateEditPage from "../content/CreateEditPage";
import PageLookup from "../lookup/PageLookup"; // Adjust the import path accordingly

function PageEditor({ config, imageList, onSavePage,createNewPage }) {
    const [showPageEdit, setShowPageEdit] = useState(false);
    const [selectedPageModel, setSelectedPageModel] = useState(null);
    const { FileProcessing, WebSiteState, DatabaseProcessing } = useAppStateContext(); 
    const [themesList, setThemesList] = useState([]);

    // Get Possible Themes
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

    useEffect(() => {
        if (createNewPage) {
            setSelectedPageModel({
                id: null,
                name: "",
                layout: "",
                keywords: [],
                topics: [],
                articles: [],
                websiteId: WebSiteState.websiteID(),
            });
            setShowPageEdit(true);
        }
    }, []);

    const pageLayouts = useMemo(() => WebSiteState.getPageLayouts() ?? [], [WebSiteState]);
    const pageKeywords = useMemo(() => WebSiteState.getPageKeywords() ?? [], [WebSiteState]);
    const pageTopics = useMemo(() => WebSiteState.getPageTopics() ?? [], [WebSiteState]);

    const handleSavePages = async (pageData) => {
        // Save Page information to database
        if (onSavePage) {
            onSavePage(pageData);
        }
        await DatabaseProcessing.savePage(pageData);
        setShowPageEdit(false);
        setSelectedPageModel(null);
    };

    const handleOnSearchArticle = async (criteria) => {
        if (!criteria.WebsiteId) {
            criteria.WebsiteId = WebSiteState.websiteID();
        }
        const articleInfo = await DatabaseProcessing.searchArticle(criteria);
        return articleInfo;
    };

    const handleArticleUpdated = async (input) => {
        if (!input.websiteId) {
            input.websiteId = WebSiteState.websiteID();
        }
        if (input.id == null) {
            //create placeholder article file
            const rootPath = `websites/${config.Site.siteName}`;
            let baseFileName = input.articlePath.replace('.html', '');
            let fileName = input.articlePath;
            let suffix = 1;
            // Check if the file exists and adjust the filename until it doesn't exist
            var fileExists = await FileProcessing.fileExists(`${rootPath}/articles`, fileName);
            while (fileExists) {
                fileName = `${baseFileName}-${suffix}.html`;
                suffix++;
                fileExists = await FileProcessing.fileExists(`${rootPath}/articles`, fileName);
            }
            await FileProcessing.saveFileData(`${rootPath}/articles`, fileName, "<p>Article Content</p>");           
            input.articlePath = fileName;
        } 
        //update article information in database
        const newArticle = await DatabaseProcessing.upsertArticle(input);
        selectedPageModel.articles.push(newArticle);
        return newArticle;
    };

    const uniqueID = () => Date.now();

    const handlePageSelected = async (page) => {
        setSelectedPageModel(page);
        setShowPageEdit(true);
    };

    return (
        <div>          
            {!selectedPageModel && !createNewPage ? (
                <PageLookup
                    showLookup={true}
                    onSelected={handlePageSelected}
                    onClose={() => {
                        setSelectedPageModel(null);
                        }
                    }
                />
            ) : selectedPageModel && (
                <div>
                    {(showPageEdit || createNewPage) && (
                        <CreateEditPage
                            pageData={selectedPageModel}
                            imageList={imageList}
                            styleSheets={themesList.map(x => x.name)}
                            columns={[{ field: "name", Header: "Name", Type: "Text" }]}
                            layouts={pageLayouts}
                            keywordOptions={pageKeywords}
                            topicsOptions={pageTopics}
                            pageSize={[2, 5, 10, 20, 50, 100]}
                            onSave={handleSavePages}
                            onArticleUpdated={handleArticleUpdated}
                            onArticleLookup={handleOnSearchArticle}
                            uniqueID={uniqueID}
                        />
                    )}
                </div>
            )}
        </div>
    );
}

export default PageEditor;