import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { NestedDynamicMenu } from '@tbirdcomponents/reactcomponents';
import { useAppStateContext } from '../../hooks/appState/useAppStateContext';
import MenuTreeEditor from './MenuTreeEditor';
import PageEditor from './PageEditor';
import PageCollectionsEditor from './PageCollectionsEditor';
import ThemeEditor from './ThemeEditor';
function SiteAdministration({ config }) {
    const [showMenuEditor, setShowMenuEditor] = useState(true);
    const [showPageEditor, setShowPageEditor] = useState(false);
    const [showPageCreator, setShowPageCreator] = useState(false);
    const [showThemeEditor, setShowThemeEditor] = useState(false);
    const [showCollectionsEditor, setShowCollectionsEditor] = useState(false);

    const setVisibility = useCallback((pageName) => {
        switch (pageName) {
            case 'menuEditor':
                setShowThemeEditor(false);
                setShowPageEditor(false);
                setShowCollectionsEditor(false);
                setShowThemeEditor(false);
                setShowPageCreator(false);
                setShowMenuEditor(true);                
                break;
            case 'pageEditor':
                setShowPageCreator(false);
                setShowThemeEditor(false);
                setShowMenuEditor(false);
                setShowCollectionsEditor(false);
                setShowThemeEditor(false);
                setShowPageEditor(true);
                break;
            case 'pageCreator':
                setShowThemeEditor(false);
                setShowMenuEditor(false);
                setShowPageEditor(false);
                setShowCollectionsEditor(false);
                setShowPageCreator(true);
                break;
            case 'collectionsEditor':
                setShowThemeEditor(false);
                setShowMenuEditor(false);
                setShowPageEditor(false);
                setShowPageCreator(false);
                setShowCollectionsEditor(true);
                break;
            case 'themeEditor':
                setShowThemeEditor(false);
                setShowMenuEditor(false);
                setShowPageEditor(false);
                setShowPageCreator(false);
                setShowCollectionsEditor(false);
                setShowThemeEditor(true);
                break;
            default:
                setShowThemeEditor(false);
                setShowPageEditor(false);
                setShowCollectionsEditor(false);
                setShowPageCreator(false);
                setShowMenuEditor(false);
                break;
        }
    }, []);

    const adminMenu = useMemo(() => {
        return [
            {
                id: "menu1",
                text: "Menu & Styling",
                menuItems: [
                    {
                        id: "menu1.1",
                        text: "Site Menu Structure",
                        onClick: (item) => {
                            setVisibility('menuEditor');
                        },
                    },
                    {
                        id: "menu1.2",
                        text: "Theme Editor",
                        onClick: (item) => {
                            setVisibility('themeEditor');
                        },
                    },
                ],
            },
            {
                id: "menu2",
                text: "Pages and Collections",
                menuItems: [
                    {
                        id: "menu2.1",
                        text: "Page Editor",
                        onClick: (item) => {
                            setVisibility('pageEditor');
                        },
                    },
                    {
                        id: "menu2.2",
                        text: "Create Page",
                        onClick: (item) => {
                            setVisibility('pageCreator');
                        },
                    },
                    {
                        id: "menu2.3",
                        text: "Configure Collections",
                        onClick: (item) => {
                            setVisibility('collectionsEditor');
                        },
                    },
                ],
            }
        ];
    },[]);

    const itemClicked = useCallback((item) => { }, []);
    const handleSavePage = useCallback((page) => {
        console.log('Save Page:', page);
    }, []);

    const renderPageContent = useMemo(() => {
        return (<div>
            <NestedDynamicMenu menuItems={adminMenu} onClick={itemClicked} keepOpen={false} />
            {showMenuEditor && 
                <div>
                  <MenuTreeEditor config={config} />
                </div>
            }
            {showPageEditor &&
                <div>
                    <PageEditor config={config} onSavePage={handleSavePage} createNewPage={false} />
                </div>
            }
            {showPageCreator &&
                <div>
                    <PageEditor config={config} onSavePage={handleSavePage} createNewPage={true} />
                </div>
            }
            {showThemeEditor &&
                <div>
                    <ThemeEditor config={config} />
                </div>
            }
            {showCollectionsEditor && <div>
                <div>
                    <PageCollectionsEditor config={config} />
                </div>
            </div>
            }
            </div >);
    }, [adminMenu, itemClicked, showMenuEditor, config, showPageEditor, handleSavePage, showPageCreator, showThemeEditor, showCollectionsEditor]);

    return (
        <div>           
            <div>{renderPageContent}</div>
        </div>
    );
}

export default SiteAdministration;
