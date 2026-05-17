// src/context/ConfigContext.js
import React, { createContext, useContext } from 'react';

const ConfigContext = createContext();

export const ConfigProvider = ({ children, value }) => (
    <ConfigContext.Provider value={value}>
        {children}
    </ConfigContext.Provider>
);

export const useConfigContext = () => useContext(ConfigContext);
