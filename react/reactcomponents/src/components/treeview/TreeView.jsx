import React, { useState, useEffect, useMemo } from "react";
import { DndProvider, useDrag, useDrop } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { Tree, useDragNode } from "@minoru/react-dnd-treeview";
import FolderIcon from "@mui/icons-material/Folder";
import FolderOpenIcon from "@mui/icons-material/FolderOpen";
import DescriptionIcon from "@mui/icons-material/Description";
import ContextMenu from "./ContextMenu";
import "./TreeView.css";
import { useCombinedRefs } from "./utils/useCombinedRefs";

const ITEM_TYPE = "TREE_ITEM";
export const MENU_CONTEXT_TYPE = Object.freeze({
    ADD_NODE: "ADD_NODE",
    ADD_PARENT_NODE: "ADD_PARENT_NODE",
    RENAME_NODE: "RENAME_NODE",
    DELETE: "DELETE",
    SEARCH_NODE: "SEARCH_NODE",
    SEARCH_PARENT: "SEARCH_PARENT",
});
function TreeView({ menuItems, onParentClick, onNodeClick, onTreeChanged, initialData, onSearchParent, onSearchNode, onDelete, onCreateNode, onRenameNode, onCustomEvent }) {
    const [treeData, setTreeData] = useState(initialData);
    const [contextMenu, setContextMenu] = useState(null);
    const [targetNode, setTargetNode] = useState(null);
    const [selectedNode, setSelectedNode] = useState(null);

    const emptyMenuTree = useMemo(() => {
        return [
            { id: -1, dbId: 0, pageId: 0, articleID: 0, parent: 0, droppable: false, text: "Empty" }
        ];
    }, []);

    useEffect(() => {
        if (initialData.length === 0) {
            setTreeData(emptyMenuTree);
        } else {
            setTreeData(initialData);
        }
    }, [initialData]);

    const getNextId = (treeData) => {
        if (treeData.length === 0) return 1; // If treeData is empty, start with 1
        const maxId = treeData.reduce((max, node) => (node.id > max ? node.id : max), treeData[0].id);
        if (maxId == -1) {
            return 1;
        } else {
            return maxId +1;
        }
    };

    const handleDrop = async (newTree) => {
        setTreeData(newTree);
        if (onTreeChanged) {
            await onTreeChanged(newTree);
        }
    };


    const handleRightClick = (event, node) => {
        event.preventDefault();
        setTargetNode(node);
        setContextMenu({ x: event.clientX, y: event.clientY });
    };

    const handleAddNode = async (nodeName,menuItem) => {
        if (!nodeName.trim()) {
            alert("Node name cannot be empty");
            return;
        }
        //preprocess menu Click
        var validateClick = true;
        if (menuItem.onClick)
        {
            validateClick = await menuItem.onClick(nodeName,menuItem);
        }
        if (!validateClick) {
            return;
        }

        const filteredTree = treeData.filter((node) => node.id !== -1);

        const selectedNodeIndex = filteredTree.findIndex((node) => node.id === selectedNode);
        var newNode = {
            id: getNextId(filteredTree),
            parent: 0,
            droppable: false,
            text: nodeName,
        };
        var newTreeData=[];
        if (selectedNodeIndex === -1) {
            //No node selected add to root
            newTreeData = [newNode, ...filteredTree];
        } else if (selectedNode === -1 || selectedNode==null)
        {
            newTreeData = [newNode];
        }else {
            const selectedNodeData = filteredTree[selectedNodeIndex];

            //Process Create Node if nothing is returned then return
            if (onCreateNode) {
                newNode = await onCreateNode(newNode);
                if (newNode == null) {
                    return;
                }
            }
            newNode.parent = selectedNodeData.id;

            //If the selected node is a parent, add the new node as a child of the selected node
            if (selectedNodeData.parent === 0) {
                newNode.parent = selectedNodeData.id;
            } else {
                newNode.parent = selectedNodeData.parent;
            }

            newTreeData = [
                ...filteredTree.slice(0, selectedNodeIndex + 1),
                newNode,
                ...filteredTree.slice(selectedNodeIndex + 1),
            ];
        }
        setTreeData(newTreeData);
        setContextMenu(null);
        if (onTreeChanged) {
            await onTreeChanged(newTreeData);
        }
    };

    const handleAddParent = async (parentNodeName, menuItem) => {

        const newPage = {
            id: getNextId(treeData),
            parent: 0,
            droppable: true,
            text: parentNodeName,
        };
        const newTreeData = treeData.filter((node) => node.id !== -1);
        setTreeData([...newTreeData, newPage]);
        setContextMenu(null);
        if (onTreeChanged) {
            await onTreeChanged([...treeData, newPage]);
        }
    };

    const handleMoveToRoot = async () => {
        const updatedTreeData = treeData.map((node) =>
            node.id === targetNode.id ? { ...node, parent: 0 } : node
        );
        setTreeData(updatedTreeData);
        setContextMenu(null);
        if (onTreeChanged) {
            await onTreeChanged(updatedTreeData);
        }
    };

    const handleCloseContextMenu = () => {
        setContextMenu(null);
    };

    const handleCustomEvent = async (node, menuItem) => {
        setContextMenu(null);
        if (selectedNode === null) return;
        const selectedNodeIndex = treeData.findIndex((node) => node.id === selectedNode);
        if (selectedNodeIndex === -1) return;
        const selectedNodeData = treeData[selectedNodeIndex];
        await onCustomEvent(selectedNodeData, menuItem);
    };
    const findNodeIndex = (id) => {
        return treeData.findIndex((node) => node.id === id);
    };

    const handleNodeClick = async (nodeData) => {
        if (!nodeData) return;

        setSelectedNode(nodeData.id);

        if (nodeData.droppable && onParentClick) {
            await onParentClick(nodeData);
        } else if (!nodeData.droppable && onNodeClick) {
            await onNodeClick(nodeData);
        }
    };
    const handleRenameNode = async (node, menuItem) => {
        if (selectedNode === null) return;
        const selectedNodeIndex = treeData.findIndex((node) => node.id === selectedNode);
        if (selectedNodeIndex === -1) return;

        const selectedNodeData = treeData[selectedNodeIndex];
        const updatedNode = { ...selectedNodeData, text: node, id: selectedNodeData.id };
        treeData[selectedNodeIndex] = updatedNode;
        setTreeData(treeData);
        setContextMenu(null);
        if (onTreeChanged) {
            await onTreeChanged(treeData);
        }
    };
    const handleDeleteNode = async (node, menuItem) => {

        if (selectedNode === null) return;

        const findAllDescendants = (nodeId, nodes) => {
            let descendants = [];
            if (nodes.length == 1) {
                return descendants;
            }
            const children = nodes.filter((node) => node.parent === nodeId && node.parent !=0);
            for (const child of children) {
                descendants.push(child.id);
                descendants = descendants.concat(findAllDescendants(child.id, nodes));
            }
            return descendants;
        };

        const allDescendants = findAllDescendants(selectedNode, treeData);
        const nodesToDelete = [selectedNode, ...allDescendants];

        //remove selected entry from tree
        const updatedTreeData = treeData.filter((node) => !nodesToDelete.includes(node.id));

        const childNodes = treeData.filter((node) => node.parent !== selectedNode);

        const selectedNodeIndex = treeData.findIndex((node) => node.id === selectedNode);
        if (selectedNodeIndex === -1) return;

        const selectedNodeData = treeData[selectedNodeIndex];

        var validateClick = true;
        if (menuItem.onClick) {
            validateClick = await menuItem.onClick(selectedNodeData, menuItem);
        }
        if (!validateClick) {
            return;
        }

        var deleted = true;
        if (onDelete) {
            deleted = await onDelete(selectedNodeData);
        }

        if (deleted) {
            if (onTreeChanged && deleted) {
                await onTreeChanged(updatedTreeData);
            }
            if (updatedTreeData.length === 0) {
                setTreeData(emptyMenuTree);
            } else {
                setTreeData(updatedTreeData);
            }
            setSelectedNode(null);
            setContextMenu(null);
        }
    };

    const moveNode = async (draggedNodeId, targetNodeId, dropPosition) => {
        const draggedIndex = findNodeIndex(draggedNodeId);
        const targetIndex = findNodeIndex(targetNodeId);

        if (draggedIndex !== -1 && targetIndex !== -1) {
            let newTreeData = [...treeData];
            const [draggedNode] = newTreeData.splice(draggedIndex, 1);
            const targetNode = treeData[targetIndex];
            if (dropPosition === "above") {
                draggedNode.parent = targetNode.parent;
                newTreeData.splice(targetIndex, 0, draggedNode);
            } else if (dropPosition === "below") {
                if (targetNode) {
                    if (targetNode.droppable) {
                        // Is there another droppable node below the target node?
                        const nextDroppableIndex = newTreeData.findIndex((node, index) => index > targetIndex && node.droppable);
                        if (nextDroppableIndex !== -1) {
                            // If so, add it as the first child of the target node
                            draggedNode.parent = targetNode.id;
                            newTreeData.splice(nextDroppableIndex, 0, draggedNode);
                        } else {
                            // Else, add it as the last child of the parent of the target node
                            const lastChildIndex = newTreeData.reduce((lastIndex, node, index) => {
                                return node.parent === targetNode.parent ? index : lastIndex;
                            }, -1);
                            draggedNode.parent = targetNode.parent;
                            newTreeData.splice(lastChildIndex + 1, 0, draggedNode);
                        }
                    } else {
                        // If the target node is not droppable, find the next sibling of the target node
                        const nextSiblingIndex = newTreeData.findIndex((node, index) => index > targetIndex && node.parent === draggedNode.parent);
                        if (nextSiblingIndex !== -1) {
                            // If the next sibling exists, place the dragged node above it
                            newTreeData.splice(nextSiblingIndex, 0, draggedNode);
                        } else {
                            // If the next sibling does not exist, place the dragged node at the end of the parent's children
                            const lastChildIndex = newTreeData.reduce((lastIndex, node, index) => {
                                return node.parent === draggedNode.parent ? index : lastIndex;
                            }, -1);
                            newTreeData.splice(lastChildIndex + 1, 0, draggedNode);
                        }
                    }
                } else {
                    //place draggedNode at the end of the tree;
                    draggedNode.parent = 0;
                    newTreeData=[...newTreeData, draggedNode];
                }
            } else if (dropPosition === "inside") {
                if (targetNode.droppable) {
                    // If the target node is droppable, place the dragged node directly inside it
                    draggedNode.parent = targetNodeId;
                    newTreeData.splice(targetIndex + 1, 0, draggedNode);
                } else {
                    // If the target node is not droppable and has the same parent as the dragged node, place the dragged node above it
                    if (targetNode.parent === draggedNode.parent) {
                        draggedNode.parent = targetNode.parent;
                        newTreeData.splice(targetIndex, 0, draggedNode);
                    } else {
                        draggedNode.parent = targetNode.parent;
                        newTreeData.splice(targetIndex + 1, 0, draggedNode);
                    }
                }
            }

            setTreeData(newTreeData);
            if (onTreeChanged) {
                await onTreeChanged(newTreeData);
            }
        }
    };

    const Node = ({ node, depth, isOpen, onToggle,onClick }) => {
        const [{ isOverAbove }, dropAbove] = useDrop({
            accept: ITEM_TYPE,
            drop: (item) => moveNode(item.id, node.id, "above"),
            collect: (monitor) => ({
                isOverAbove: monitor.isOver({ shallow: true }) && monitor.getItemType() === ITEM_TYPE,
            }),
        });

        const [{ isOverBelow }, dropBelow] = useDrop({
            accept: ITEM_TYPE,
            drop: (item) => moveNode(item.id, node.id, "below"),
            collect: (monitor) => ({
                isOverBelow: monitor.isOver({ shallow: true }) && monitor.getItemType() === ITEM_TYPE,
            }),
        });

        const [{ isOverInside }, dropInside] = useDrop({
            accept: ITEM_TYPE,
            drop: (item) => moveNode(item.id, node.id, "inside"),
            collect: (monitor) => ({
                isOverInside: monitor.isOver({ shallow: true }) && monitor.getItemType() === ITEM_TYPE,
            }),
        });

        const [{ isDragging }, drag] = useDrag({
            type: ITEM_TYPE,
            item: { id: node.id },
            collect: (monitor) => ({
                isDragging: monitor.isDragging(),
            }),
        });

        const combinedRef = useCombinedRefs(dropInside, drag);

        const handleClick = () => {
            if (onClick) {
                onClick(node);
            }
        };

        return (
            <div>
                {node.droppable && <div ref={dropAbove} style={{ height: 5, background: isOverAbove ? "rgba(0, 0, 255, 0.1)" : "transparent" }} />}
                <div
                    ref={combinedRef}
                    className={`tree-node ${selectedNode === node.id ? "selected" : ""}`}
                    style={{
                        marginInlineStart: depth * 20,
                        opacity: isDragging ? 0.5 : 1,
                        cursor: "pointer",
                        background: isOverInside ? "rgba(0, 0, 255, 0.1)" : "transparent",
                    }}
                    onContextMenu={(event) => handleRightClick(event, node)}
                    onClick={handleClick}
                    title={node?.itemTitle ?? node?.title ?? node?.pageName ?? node?.text ?? undefined}
                >
                    {node.droppable ? (
                        <span className="tree-toggle" onClick={onToggle}>
                            {isOpen ? <FolderOpenIcon className="tree-icon page-icon" /> : <FolderIcon className="tree-icon page-icon" />}
                        </span>
                    ) : (
                        <DescriptionIcon className="tree-icon article-icon" />
                    )}
                    {node.text}
                </div>
                <div ref={dropBelow} style={{ height: 5, background: isOverBelow ? "rgba(0, 0, 255, 0.1)" : "transparent" }} />
            </div>
        );
    };

    const handleSearching = async (callback,menuItem) => {
        if (selectedNode === null) return;
        const selectedNodeIndex = treeData.findIndex((node) => node.id === selectedNode);
        if (selectedNodeIndex === -1) return;
        const selectedNodeData = treeData[selectedNodeIndex];
        const searchResult = await callback(selectedNodeData, treeData, menuItem);
    };
    return (
        <DndProvider backend={HTML5Backend}>
            <div className="tree-container" style={{ height: 400 }}>
                <Tree
                    tree={treeData}
                    sort={false}
                    insertDroppableFirst={false}
                    rootId={0}
                    render={(node, { depth, isOpen, onToggle }) => (
                        <Node key={node.id} node={node} depth={depth} isOpen={isOpen} onToggle={onToggle} onClick={handleNodeClick} />
                    )}
                    dragPreviewRender={(monitorProps) => <div>{monitorProps.item.text}</div>}
                    onDrop={handleDrop}
                />
                <ContextMenu
                    menuItems={menuItems }
                    position={contextMenu}
                    onDelete={handleDeleteNode}
                    onAddNode={handleAddNode}
                    onAddParent={handleAddParent}
                    onMoveToRoot={handleMoveToRoot}
                    onClose={handleCloseContextMenu}
                    onRenameNode={handleRenameNode}
                    onCustomEvent={async (nodeName,menuItem) => {
                        if (onCustomEvent) {
                            await handleCustomEvent(nodeName,menuItem);
                        }
                    }}
                    onSearchNode={async (node,menuItem) => {
                        handleCloseContextMenu();
                        if (onSearchNode) {
                            await handleSearching(onSearchNode,menuItem);
                        }
                    }}
                    onSearchParent={async (node,menuItem) => {
                        handleCloseContextMenu();
                        if (onSearchParent) {
                            await handleSearching(onSearchParent,menuItem);
                        }
                    }}
                />
            </div>
        </DndProvider>
    );
}

export default TreeView;
