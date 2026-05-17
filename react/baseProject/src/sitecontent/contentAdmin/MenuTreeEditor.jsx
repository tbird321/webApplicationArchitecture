import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext'; // Adjust the import path accordingly
import { TreeView, MENU_CONTEXT_TYPE } from '@tbirdcomponents/reactcomponents';
import PageLookup from '../lookup/PageLookup'; // Adjust the import path accordingly'
function MenuTreeEditor({ config }) {
    const [showSitePagesEdit, setShowSitePagesEdit] = useState(false);
    const [menuObject, setMenuObject] = useState([]); // State to track if config is loaded.
    const [selectedMenuItem, setSelectedMenuItem] = useState(null); // State to track if config is loaded.
    const { FileProcessing, WebSiteState } = useAppStateContext();    
   
    useEffect(() => {
        const getPageInfo = async () => {
            // Early return if siteInfo is null or undefined
            //WebSiteState.setShowSpinner(true);
            const rootPath = `websites/${config.Site.siteName}`;
            let menuInfo = await FileProcessing.getFileDataObject(rootPath, config.Site.menuConfigFileName);
            if (!menuInfo) {
                menuInfo = [];
            }
            setMenuObject(menuInfo);
        }
        getPageInfo();
    }, [FileProcessing, WebSiteState, config]);

    const validateNodeName = (nodename) => {
        if (!nodename.trim()) {
            alert("name cannot be empty");
            return false;
        }
        return true;
    };   

    const dynamicMenuItems = useMemo(() => {
        var cmdButtons = [
            {
                type: MENU_CONTEXT_TYPE?.SEARCH_NODE, label: "Attach Page", onClick: (nodeName, menuItem) => {
                    alert('Attach Page' + nodeName);
                    return true;
                }
            },
            {
                type: MENU_CONTEXT_TYPE?.RENAME_NODE, label: "Rename Menu Item", onClick: (nodename, menuItem) => {

                    return validateNodeName(nodename);
                }
            },
            {
                type: MENU_CONTEXT_TYPE?.ADD_NODE, label: "Add Menu Item", onClick: (nodename, menuItem) => {

                    return validateNodeName(nodename);
                }
            },
            {
                type: MENU_CONTEXT_TYPE?.ADD_PARENT_NODE, label: "Add Parent Item", onClick: (nodename, menuItem) => {

                    return validateNodeName(nodename);
                }
            },
            {
                type: MENU_CONTEXT_TYPE?.DELETE, label: "Delete", onClick: (nodename, menuItem) => {

                    return true;
                }
            }
        ];
        return cmdButtons;
    }, []);

    const handleFolderClick = (node) => { };
    const handleArticleClick = (node) => { };
    const handleCustomEvent = (node, type) => {
       
    };
    const handleTreeChanged = useCallback((treeData) => {
        const saveMenuInfo = async (treeData) => {
            const rootPath = 'websites/' + config.Site.siteName;
            await FileProcessing.saveFileData(rootPath, config.Site.menuConfigFileName, JSON.stringify(treeData));
        }
        saveMenuInfo(treeData);        
    }, [FileProcessing, config.Site.menuConfigFileName, config.Site.siteName]);

    const handleSearchPage = (node) => { };
    const handleSearchArticle = (node, treeData, menuItem) => {
        switch (menuItem.type) {
            case MENU_CONTEXT_TYPE?.SEARCH_NODE:
                //Open Modal for Page Search
                console.log('Search Article' + node);
                setShowSitePagesEdit(true);
                setSelectedMenuItem(node);
                break;
            case MENU_CONTEXT_TYPE?.SEARCH_PARENT:
                break;
            default:
                break;
        }
    };
    const handleCreateArticle = (node) => {
        return node;
    };
    const handleCreatePage = (node) =>
    {
        console.log(node);
        return node;
    }

    const handleAttachPage = useCallback(async (page) => {
        //update MenuItem with Page Id - menuObject        
        if (menuObject && menuObject.length > 0) {
            console.log(selectedMenuItem);
            menuObject.forEach((item) => {
                console.log(item.id);
                
                if (item.id === selectedMenuItem.id) {
                    item.pageId = page.id;
                    item.pageName= page.name;
                    item.itemTitle = "attached: " + page.name;
                }                
            });
            const rootPath = 'websites/' + config.Site.siteName;
            await FileProcessing.saveFileData(rootPath, config.Site.menuConfigFileName, JSON.stringify(menuObject));
            setMenuObject(menuObject);
        }
        
        //Save Menu Info
        setShowSitePagesEdit(false);
    }, [FileProcessing, config.Site.menuConfigFileName, config.Site.siteName, menuObject, selectedMenuItem]);

    const initialRenderData = useMemo(() => {
        const mapNode = (node) => {
            if (!node) return node;
            const title = typeof node?.itemTitle === 'string' ? node.itemTitle.trim() : '';
            return { ...node, itemTitle: title };
        };
        return (menuObject || []).map(mapNode);
    }, [menuObject]);

    const renderPageContent = useMemo(() => {
        return (<div>
            <TreeView menuItems={dynamicMenuItems} initialData={initialRenderData} onParentClick={handleFolderClick} onNodeClick={handleArticleClick}
                onTreeChanged={handleTreeChanged} onSearchParent={handleSearchPage} onSearchNode={handleSearchArticle}
                onCustomEvent={handleCustomEvent}
                onCreateNode={handleCreateArticle} onCreateParent={handleCreatePage} />
            <div>

            </div>
        </div >);
    }, [dynamicMenuItems, handleTreeChanged, initialRenderData]);
    return (
        <div>
            <div>{renderPageContent}</div>
            {showSitePagesEdit && <PageLookup showLookup={showSitePagesEdit} currentPage={
                selectedMenuItem?.pageName !== undefined
                    ? 'Current Page: ' + selectedMenuItem.pageName
                    : 'None Selected'
            }
                onClose={() => { setShowSitePagesEdit(false); }}
                onSearch={() => { setShowSitePagesEdit(false); }}
                onSelected={handleAttachPage}
            />}

        </div>
    );
}

export default MenuTreeEditor;
