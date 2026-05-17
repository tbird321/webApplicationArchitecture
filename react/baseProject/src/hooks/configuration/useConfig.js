// src/hooks/useConfig.js
import { useState, useEffect } from 'react';
import { Amplify } from 'aws-amplify';

const useConfig = () => {
    const [configLoaded, setConfigLoaded] = useState(false);
    const [config, setConfig] = useState(null);

    useEffect(() => {
        const fetchConfig = async () => {
            try {
                const response = await fetch('/config.json');
                const newConfig = await response.json();
                if (!Amplify.configure.done) {
                    await Amplify.configure(newConfig.AWS);
                    setConfig(newConfig);
                }
                setConfigLoaded(true);
            } catch (error) {
                console.error("Error loading config:", error);
            }
        };

        fetchConfig();
    }, []);

    return { configLoaded, config };
};

export default useConfig;
