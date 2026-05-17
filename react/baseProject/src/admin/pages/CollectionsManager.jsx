import React from 'react';
import { useOutletContext } from 'react-router-dom';
import { Box } from '@mui/material';
import PageCollectionsEditor from '../../sitecontent/contentAdmin/PageCollectionsEditor';

function CollectionsManager() {
    const { config } = useOutletContext() ?? {};
    return (
        <Box>
            <PageCollectionsEditor config={config} />
        </Box>
    );
}

export default CollectionsManager;
