import React, { useEffect, useState } from 'react';
import Box from '@mui/material/Box';
import Drawer from '@mui/material/Drawer';
import Button from '@mui/material/Button';
import List from '@mui/material/List';
import Divider from '@mui/material/Divider';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemText from '@mui/material/ListItemText';
import InboxIcon from '@mui/icons-material/MoveToInbox';

function CollectionNavigator({ onPageNameChange, collectionInfo, openCollection, onToggleCollection }) {
    const [tempPages, setTempPages] = useState([]);
    const [collectionName, setCollectionName] = useState('');

    useEffect(() => {
        // Assuming collectionInfo.pages is the array you're interested in
        setTempPages(collectionInfo?.pages || []);
        setCollectionName(collectionInfo?.collection.name || '');
        console.log(collectionInfo)
    }, [collectionInfo]);

    const handleToggleCollection = (bool) => {
        onToggleCollection(bool);
    };
    const handlePageNameChange = (input) => {
        onPageNameChange(input);
    };

    const DrawerList = (
        <Box sx={{ width: 250 }} role="presentation">
            <List>
                <ListItem>
                        <ListItemText primary={collectionName} />
                </ListItem>
            </List>
            <Divider />
            <List>
                {tempPages.map((page, index) => (
                    <ListItem key={index} disablePadding>
                        <ListItemButton onClick={() => handlePageNameChange(page.name)}>
                            <ListItemText primary={page.name} />
                        </ListItemButton>
                    </ListItem>
                ))}
            </List>
        </Box>
    );

    return (
        <div>
            <Drawer open={openCollection} onClose={() => handleToggleCollection(false)}>
                {DrawerList}
            </Drawer>
        </div>
    );
}

export default CollectionNavigator;
