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
                    const awsConfig = { ...newConfig.AWS };
                    if (newConfig.api_key && awsConfig.endpoints) {
                        const apiKey = newConfig.api_key;
                        awsConfig.endpoints = awsConfig.endpoints.map(ep => ({
                            ...ep,
                            custom_header: async () => ({ 'X-Api-Key': apiKey })
                        }));
                    }
                    await Amplify.configure(awsConfig);
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
