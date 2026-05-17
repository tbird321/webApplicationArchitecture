// MenuEditor.jsx
import React, { useState,useEffect } from "react";
import NestedDynamicMenu from "./NestedDynamicMenu";
import MenuItemEditor from "./MenuItemEditor";
import Button from "@mui/material/Button";
import ModalDialog from "../ModalDialog";
import { SearchGrid } from "../data";

const pageSize = [2, 5, 10, 20, 50, 100];

function MenuEditor({ menuItems, onItemsUpdated, onItemNotAdded, onSaveChanges, pageSize, pages, dataGridColumns, onSearch, keywordOptions, topicsOptions }) {
    const [menuData, setMenuData] = useState(menuItems);
    const [editingItem, setEditingItem] = useState(null);
    const [modifyingItem, setModifyingItem] = useState(null);
    const [uniqueIds, setUniqueIds] = useState({});
    const [openSearch, setOpenSearch] = useState(false);
    const [tempPages, setTempPages] = useState(null);

    const [currentPage, setCurrentPage] = useState();
    const dialogStyle = { width: "95%" };

    const handleClose = () => setOpenSearch(false);
    const handleOpenSearch = () => {
        setOpenSearch(true);
    };
    const handleSetPages = () => {
        setTempPages(pages);
    };
    useEffect(() => {
        const newUniqueIds={};
        menuItems?.forEach((item)=>{
            newUniqueIds[item.id]=true;
            item.menuItems?.forEach((childitem)=>{
                newUniqueIds[childitem.id]=true;
            });
        });
        setUniqueIds(newUniqueIds);
    }, [menuItems]);

    const updateMenuItem = (updatedItem) => {
        if (uniqueIds[updatedItem.id])
        {
            updatedItem.id=generateUniqueId();
            uniqueIds[updatedItem.id]=true;
            setUniqueIds(uniqueIds);
        }
        else
        {
            uniqueIds[updatedItem.id]=true;
            setUniqueIds(uniqueIds);
        }

        const updatedMenuData = updateMenuRecursively(menuData, updatedItem);
        setMenuData(updatedMenuData);
        if (onItemsUpdated)
        {
            onItemsUpdated(updatedMenuData);
        }
    };

    const updateMenuRecursively = (menu, updatedItem) => {
        return menu?.map((item) => {
            if (item.id === editingItem.id) {
                return updatedItem;
            } else if (item.menuItems) {
                const updatedMenuItems = updateMenuRecursively(item.menuItems, updatedItem);
                return {
                    ...item,
                    menuItems: updatedMenuItems,
                };
            }
            return item;
        });
    };

    const deleteMenuItem=()=>{
        if (editingItem)
        {
            const updatedMenuData = menuData?.map(item => {
            // If the current item has the id we're looking for, delete it.
                if (item.id === editingItem.id) return null;

                // If the current item has a 'menuItems' property, check there too.
                if (item.menuItems) {
                    const updatedMenuItems = item.menuItems.filter(subItem => subItem.id !== editingItem.id);
                    return {...item, menuItems: updatedMenuItems};
                }

                // If the current item didn't match the id and didn't have 'menuItems', return it unchanged.
                return item;
            }).filter(Boolean); // This filters out any null values, effectively deleting items with matching ids.
            delete uniqueIds[editingItem.id];
            setUniqueIds({...uniqueIds});
            setMenuData(updatedMenuData);
        }
    };

    const onAddBelow = () => {
        if (editingItem && modifyingItem)
        {
            var parentId=editingItem.id;
            const newMenuItem = {...modifyingItem};

            if (uniqueIds[modifyingItem.id])
            {
                newMenuItem.id=generateUniqueId();
                uniqueIds[newMenuItem.id]=true;
                setUniqueIds(uniqueIds);
            }
            else
            {
                uniqueIds[newMenuItem.id]=true;
                setUniqueIds(uniqueIds);
            }

            newMenuItem.menuItems=[];
            const updatedMenuData = menuData?.map((item) => {
                if (item.id === parentId) {
                    return {
                        ...item,
                        menuItems: [...(item.menuItems || []), newMenuItem],
                    };
                }else{
                    //check children
                    let foundIndex=-1;
                    item?.menuItems.forEach((childitem,index)=>{
                        if (childitem.id===parentId)
                        {
                            //add into item.menuItems below current item
                            foundIndex=index;
                        }
                    });
                    if (foundIndex>=0)
                    {
                        //Add item at index;
                        item.menuItems.splice(foundIndex+1,0,newMenuItem);
                        setModifyingItem(newMenuItem);
                    }
                }
                return item;
            });
            setMenuData(updatedMenuData);
        }
    };

    const handleOnClick = (clickedItem) => {
        setEditingItem(clickedItem);
        setModifyingItem(clickedItem);
    };

    const handleSaveChanges=()=>{
        if (onSaveChanges)
        {
            onSaveChanges(menuData);
        }
    };

    function generateUniqueId() {
        let newId;
        do {
            newId = "id" + Math.floor(Math.random() * 1000); // Adjust the range and prefix as needed
        } while (uniqueIds[newId]);
        return newId;
    }
    const handleRowClick = (selectionModel) => {
        setCurrentPage(selectionModel.row);
        setOpenSearch(false);
    };
    const handleSearch = (searchCrit) => {
        if (onSearch) {
            var searchResults = [...onSearch(searchCrit)];
            setTempPages(searchResults);
            return searchResults;
        } else {
            return [];
        }
    };

    const addNewRootItem =()=>{
        const newMenuItem = {...modifyingItem === null?{text:"newItem"}:modifyingItem};
        if (!newMenuItem.id || uniqueIds[newMenuItem.id])
        {
            newMenuItem.id=generateUniqueId();
            uniqueIds[newMenuItem.id]=true;
            setUniqueIds({...uniqueIds});
        }else
        {
            uniqueIds[newMenuItem.id]=true;
            setUniqueIds(uniqueIds);
        }

        if (newMenuItem.id && newMenuItem.id!=="")
        {
            newMenuItem.menuItems=[];
            // Use the spread operator to create a new array with the added item
            const updatedMenuData = [...menuData, newMenuItem];
            setMenuData(updatedMenuData);
        }else
        {
            newMenuItem.id="newItem";
            newMenuItem.menuItems=[];
            newMenuItem.text="New Menu";
            const updatedMenuData = [...menuData, newMenuItem];
            setMenuData(updatedMenuData);
        }
        setEditingItem(null);
        setModifyingItem(null);
    };

    return (
        <div>
            <div style={{display: "flex", flexDirection: "row",paddingBottom: "5px",gap:"5px"}}>
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
                <div style={{display: "flex", flexDirection: "row",paddingTop:"10px"}}>
                    <div>
                        <div>Selected Context</div>
                        <MenuItemEditor
                            disabled={true}
                            item={editingItem}
                            onUpdate={(updatedItem) => {
                                updateMenuItem(updatedItem);
                            }}
                            onItemChanged={(itemChanged)=>{
                                setModifyingItem(itemChanged);
                            }}
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
                            onItemChanged={(itemChanged)=>{
                                setModifyingItem(itemChanged);
                            }}
                            onAddBelow={onAddBelow}
                            onDeleteMenuItem={deleteMenuItem}
                            onCanceled={()=>{
                                setEditingItem(null);
                            }}
                        />
                    </div>
                </div>
            </div>
            }
            <NestedDynamicMenu menuItems={menuData} onMenuItemClick={updateMenuItem} onClick={handleOnClick} keepOpen={true} />
            <ModalDialog open={openSearch} onClose={handleClose} dialogStyle={dialogStyle} aria-labelledby="modal-modal-title" aria-describedby="modal-modal-description" >
                <SearchGrid
                    onRowSelect={handleRowClick}
                    onSearch={handleSearch}
                    pageSize={pageSize}
                    columns={dataGridColumns}
                    items={tempPages == null ? [] : tempPages}
                    handleSetPages={handleSetPages}
                    keywordOptions={keywordOptions??[]}
                    topicsOptions={topicsOptions??[]}
                />
            </ModalDialog>
        </div>
    );
}

export default MenuEditor;
