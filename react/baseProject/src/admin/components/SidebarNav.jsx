import React, { useState, useMemo } from 'react';
import {
    List,
    ListItemButton,
    ListItemIcon,
    ListItemText,
    Collapse,
    Toolbar,
    Divider,
    Typography
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ArticleIcon from '@mui/icons-material/Article';
import DescriptionIcon from '@mui/icons-material/Description';
import MenuIcon from '@mui/icons-material/Menu';
import CollectionsIcon from '@mui/icons-material/Collections';
import PaletteIcon from '@mui/icons-material/Palette';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';

const NAV_GROUPS = [
    {
        id: 'dashboard',
        type: 'item',
        label: 'Dashboard',
        path: '/admin/dashboard',
        icon: DashboardIcon
    },
    {
        id: 'content',
        type: 'group',
        label: 'Content',
        children: [
            { label: 'Pages', path: '/admin/pages', icon: ArticleIcon },
            { label: 'Articles', path: '/admin/articles', icon: DescriptionIcon }
        ]
    },
    {
        id: 'structure',
        type: 'group',
        label: 'Structure',
        children: [
            { label: 'Menus', path: '/admin/menus', icon: MenuIcon },
            { label: 'Collections', path: '/admin/collections', icon: CollectionsIcon }
        ]
    },
    {
        id: 'appearance',
        type: 'group',
        label: 'Appearance',
        children: [
            { label: 'Themes', path: '/admin/themes', icon: PaletteIcon }
        ]
    }
];

function SidebarNav({ activePath, onNavigate }) {
    const initialOpenState = useMemo(() => {
        const state = {};
        NAV_GROUPS.forEach((g) => {
            if (g.type === 'group') {
                state[g.id] = g.children.some((c) => activePath?.startsWith(c.path));
            }
        });
        return state;
    }, [activePath]);

    const [openGroups, setOpenGroups] = useState(initialOpenState);

    const toggleGroup = (id) => {
        setOpenGroups((prev) => ({ ...prev, [id]: !prev[id] }));
    };

    const isActive = (path) => activePath === path || activePath?.startsWith(path + '/');

    return (
        <div>
            <Toolbar>
                <Typography variant="h6" noWrap>
                    Admin
                </Typography>
            </Toolbar>
            <Divider />
            <List>
                {NAV_GROUPS.map((group) => {
                    if (group.type === 'item') {
                        const Icon = group.icon;
                        return (
                            <ListItemButton
                                key={group.id}
                                selected={isActive(group.path)}
                                onClick={() => onNavigate(group.path)}
                            >
                                <ListItemIcon><Icon /></ListItemIcon>
                                <ListItemText primary={group.label} />
                            </ListItemButton>
                        );
                    }
                    const expanded = openGroups[group.id] ?? false;
                    return (
                        <React.Fragment key={group.id}>
                            <ListItemButton onClick={() => toggleGroup(group.id)}>
                                <ListItemText primary={group.label} primaryTypographyProps={{ variant: 'overline' }} />
                                {expanded ? <ExpandLess /> : <ExpandMore />}
                            </ListItemButton>
                            <Collapse in={expanded} timeout="auto" unmountOnExit>
                                <List component="div" disablePadding>
                                    {group.children.map((child) => {
                                        const Icon = child.icon;
                                        return (
                                            <ListItemButton
                                                key={child.path}
                                                selected={isActive(child.path)}
                                                onClick={() => onNavigate(child.path)}
                                                sx={{ pl: 4 }}
                                            >
                                                <ListItemIcon><Icon /></ListItemIcon>
                                                <ListItemText primary={child.label} />
                                            </ListItemButton>
                                        );
                                    })}
                                </List>
                            </Collapse>
                        </React.Fragment>
                    );
                })}
            </List>
        </div>
    );
}

export default SidebarNav;
