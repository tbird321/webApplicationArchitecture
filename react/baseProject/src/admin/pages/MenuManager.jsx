import React, { useEffect } from 'react';
import { useOutletContext } from 'react-router-dom';
import { Box, Typography } from '@mui/material';
import MenuTreeEditor from '../../sitecontent/contentAdmin/MenuTreeEditor';

function MenuManager() {
    const { config } = useOutletContext() ?? {};

    useEffect(() => {
        const handleBeforeUnload = (e) => {
            e.preventDefault();
            e.returnValue = '';
        };
        window.addEventListener('beforeunload', handleBeforeUnload);
        return () => window.removeEventListener('beforeunload', handleBeforeUnload);
    }, []);

    return (
        <Box>
            <Typography variant="h4" gutterBottom>Menu Structure</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Changes auto-save to the site menu file. Drag to reorder, right-click for options.
            </Typography>
            <MenuTreeEditor config={config} />
        </Box>
    );
}

export default MenuManager;
