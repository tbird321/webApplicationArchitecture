import React from 'react';
import { useOutletContext } from 'react-router-dom';
import { Box, Typography } from '@mui/material';
import ThemeEditor from '../../sitecontent/contentAdmin/ThemeEditor';

function ThemeManager() {
    const { config } = useOutletContext() ?? {};
    return (
        <Box>
            <Typography variant="h4" gutterBottom>Themes</Typography>
            <ThemeEditor config={config} />
        </Box>
    );
}

export default ThemeManager;
