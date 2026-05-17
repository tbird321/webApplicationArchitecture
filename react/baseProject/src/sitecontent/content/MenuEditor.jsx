// MenuEditor.jsx
import React, { useState, useEffect, useCallback } from "react";
import Button from "@mui/material/Button";
import { ModalDialog, NestedDynamicMenu, SearchGrid, isPromise } from '@tbirdcomponents/reactcomponents'; // Adjust the import path accordingly
import MenuItemEditor from "./MenuItemEditor";
function MenuEditor({ menuItems, onItemsUpdated, onItemNotAdded, onSaveChanges, pageSize, pages, dataGridColumns, onSearch, keywordOptions, topicsOptions, onSearchType, searchType }) {
    const [menuData, setMenuData] = useState(menuItems || []);
    const [editingItem, setEditingItem] = useState(null);
    const [modifyingItem, setModifyingItem] = useState(null);
    const [uniqueIds, setUniqueIds] = useState({});
    const [openSearch, setOpenSearch] = useState(false);
    const [tempResults, setTempResults] = useState(null);

    const [currentPage, setCurrentPage] = useState(); // retained for future UI hints; safe to ignore
    const dialogStyle = { width: "95%" };

    const handleClose = () => setOpenSearch(false);
    const handleOpenSearch = () => {
        setOpenSearch(true);
    };
    const handleSetPages = () => {
        setTempResults(pages);
    };
    // Normalize tree: ensure stable ids map and default titles
    const normalizeMenuTree = useCallback((menu) => {
        const normalizeItem = (item) => {
            if (!item) return item;
            const normalized = {
                ...item,
                itemTitle: item?.itemTitle ?? item?.pageName ?? item?.text ?? "",
            };
            if (Array.isArray(item.menuItems) && item.menuItems.length > 0) {
                normalized.menuItems = item.menuItems.map(normalizeItem);
            }
            return normalized;
        };
        return (menu || []).map(normalizeItem);
    }, []);

    useEffect(() => {
        // keep local state in sync when parent provides new menu items
        const normalized = normalizeMenuTree(menuItems || []);
        setMenuData(normalized);
        const newUniqueIds = {};
        const walk = (item) => {
            if (!item) return;
            newUniqueIds[item.id] = true;
            if (Array.isArray(item.menuItems)) {
                item.menuItems.forEach(walk);
            }
        };
        (normalized || []).forEach((item) => {
            newUniqueIds[item.id] = true;
            walk(item);
        });
        setUniqueIds(newUniqueIds);
    }, [menuItems, normalizeMenuTree]);

    const updateMenuItem = (updatedItem) => {
        const ensuredItem = { ...updatedItem };
        if (uniqueIds[ensuredItem.id] === undefined) {
            ensuredItem.id = generateUniqueId();
        }
        // Default title if missing
        if (!ensuredItem.itemTitle || ensuredItem.itemTitle.trim() === "") {
            ensuredItem.itemTitle = ensuredItem.pageName ?? ensuredItem.text ?? "";
        }
        setUniqueIds((prev) => ({ ...prev, [ensuredItem.id]: true }));

        const updatedMenuData = updateMenuRecursively(menuData, ensuredItem);
        setMenuData(updatedMenuData);
        if (onItemsUpdated) {
            onItemsUpdated(updatedMenuData);
        }
    };
    const handleSearchType = (string) => {
        onSearchType(string);
    };

    const updateMenuRecursively = (menu, updatedItem) => {
        return (menu || []).map((item) => {
            if (!item) return item;
            if (item.id === editingItem?.id) {
                return { ...updatedItem };
            }
            if (Array.isArray(item.menuItems) && item.menuItems.length > 0) {
                const updatedMenuItems = updateMenuRecursively(item.menuItems, updatedItem);
                if (updatedMenuItems !== item.menuItems) {
                    return { ...item, menuItems: updatedMenuItems };
                }
            }
            return item;
        });
    };

    const deleteMenuItem = () => {
        if (editingItem) {
            const updatedMenuData = menuData?.map(item => {
                // If the current item has the id we're looking for, delete it.
                if (item.id === editingItem.id) return null;

                // If the current item has a 'menuItems' property, check there too.
                if (item.menuItems) {
                    const updatedMenuItems = item.menuItems.filter(subItem => subItem.id !== editingItem.id);
                    return { ...item, menuItems: updatedMenuItems };
                }

                // If the current item didn't match the id and didn't have 'menuItems', return it unchanged.
                return item;
            }).filter(Boolean); // This filters out any null values, effectively deleting items with matching ids.
            delete uniqueIds[editingItem.id];
            setUniqueIds({ ...uniqueIds });
            setMenuData(updatedMenuData);
        }
    };

    const onAddBelow = () => {
        if (editingItem && modifyingItem) {
            const parentId = editingItem.id;
            const baseItem = { ...modifyingItem };
            const id = uniqueIds[baseItem.id] ? generateUniqueId() : baseItem.id;
            const newMenuItem = { ...baseItem, id, menuItems: [] };
            setUniqueIds((prev) => ({ ...prev, [newMenuItem.id]: true }));

            const addBelowRecursive = (menu) => {
                let changed = false;
                const next = (menu || []).map((item) => {
                    if (!item) return item;
                    if (item.id === parentId) {
                        changed = true;
                        return { ...item, menuItems: [ ...(item.menuItems || []), newMenuItem ] };
                    }
                    if (Array.isArray(item.menuItems) && item.menuItems.length > 0) {
                        const child = addBelowRecursive(item.menuItems);
                        if (child !== item.menuItems) {
                            changed = true;
                            return { ...item, menuItems: child };
                        }
                    }
                    return item;
                });
                return changed ? next : menu;
            };

            const updatedMenuData = addBelowRecursive(menuData);
            setModifyingItem(newMenuItem);
            setMenuData(updatedMenuData);
        }
    };

    const handleOnClick = (clickedItem) => {
        setEditingItem(clickedItem);
        setModifyingItem(clickedItem);
    };

    const handleSaveChanges = () => {
        if (onSaveChanges) {
            onSaveChanges(menuData);
        }
    };

    function generateUniqueId() {
        let newId;
        do {
            newId = `id_${Date.now()}_${Math.floor(Math.random() * 100000)}`;
        } while (uniqueIds[newId]);
        return newId;
    }
    const handleRowClick = (selectionModel) => {
        const selected = selectionModel.row;
        setCurrentPage(selected);
        const updated = {
            ...(modifyingItem || {}),
            pageId: selected.id,
            pageName: selected.name,
            // Generic, user-facing title for hover or labeling
            itemTitle: (modifyingItem && modifyingItem.itemTitle) ? modifyingItem.itemTitle : selected.name,
        };
        setModifyingItem(updated);
        updateMenuItem(updated);
        setOpenSearch(false);
    };
    const handleSearch = (searchCrit) => {
        if (onSearch) {

            var results = onSearch(searchCrit);
            var searchResults = [];
            if (isPromise(results)) {
                results.then((pageInfo) => {
                    searchResults = [...pageInfo];
                    setTempResults(searchResults);
                });
            } else {
                searchResults = [...results];
                setTempResults(searchResults);
            }            
            return searchResults;
        } else {
            return [];
        }
    };

    const addNewRootItem = () => {
        const newMenuItem = { ...modifyingItem === null ? { text: "newItem" } : modifyingItem };
        if (!newMenuItem.id || uniqueIds[newMenuItem.id]) {
            newMenuItem.id = generateUniqueId();
            uniqueIds[newMenuItem.id] = true;
            setUniqueIds({ ...uniqueIds });
        } else {
            uniqueIds[newMenuItem.id] = true;
            setUniqueIds(uniqueIds);
        }

        if (newMenuItem.id && newMenuItem.id !== "") {
            newMenuItem.menuItems = [];
            newMenuItem.itemTitle = newMenuItem.itemTitle ?? newMenuItem.pageName ?? newMenuItem.text ?? "";
            // Use the spread operator to create a new array with the added item
            const updatedMenuData = [...menuData, newMenuItem];
            setMenuData(updatedMenuData);
        } else {
            newMenuItem.id = "newItem";
            newMenuItem.menuItems = [];
            newMenuItem.text = "New Menu";
            newMenuItem.itemTitle = newMenuItem.text;
            const updatedMenuData = [...menuData, newMenuItem];
            setMenuData(updatedMenuData);
        }
        setEditingItem(null);
        setModifyingItem(null);
    };

    return (
        <div>
            <div style={{ display: "flex", flexDirection: "row", paddingBottom: "5px", gap: "5px" }}>
                <Button
                    onClick={addNewRootItem}
                    color="primary"
                    variant="contained"
                >
                    Add New Root Item
                </Button>
                <Button
                    onClick={handleSaveChanges}
                    color="primary"
                    variant="contained"
                >
                    Save Changes
                </Button>
            </div>
            {editingItem &&
                <div>
                    <div style={{ display: "flex", flexDirection: "row", paddingTop: "10px" }}>
                        <div>
                            <div>Selected Context</div>
                            <MenuItemEditor
                                disabled={true}
                                item={editingItem}
                                onUpdate={(updatedItem) => {
                                    updateMenuItem(updatedItem);
                                }}
                                onItemChanged={(itemChanged) => {
                                    setModifyingItem(itemChanged);
                                }}
                                onSearchType={handleSearchType}
                            />
                        </div>
                        <div>
                            <div>Editing Context</div>
                            <MenuItemEditor
                                onOpenSearch={handleOpenSearch}
                                item={editingItem}
                                onUpdate={(updatedItem) => {
                                    updateMenuItem(updatedItem);
                                }}
                                onItemChanged={(itemChanged) => {
                                    setModifyingItem(itemChanged);
                                }}
                                onAddBelow={onAddBelow}
                                onDeleteMenuItem={deleteMenuItem}
                                onCanceled={() => {
                                    setEditingItem(null);
                                }}
                                onSearchType={handleSearchType}
                            />
                        </div>
                    </div>
                </div>
            }
            <NestedDynamicMenu menuItems={menuData} onMenuItemClick={updateMenuItem} onClick={handleOnClick} keepOpen={true} showTitles={true} />
            <ModalDialog open={openSearch} onClose={handleClose} dialogStyle={dialogStyle} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <SearchGrid
                    onRowSelect={handleRowClick}
                    onSearch={handleSearch}
                    pageSize={pageSize}
                    columns={dataGridColumns}
                    items={tempResults == null ? [] : tempResults}
                    handleSetPages={handleSetPages}
                    keywordOptions={keywordOptions ?? []}
                    topicsOptions={topicsOptions ?? []}
                    searchType={searchType}
                />
            </ModalDialog>
        </div>
    );
}

export default MenuEditor;
