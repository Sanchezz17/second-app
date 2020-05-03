import React from 'react';
import styles from './style.module.css';
import Field from '../Field';
import { DELAY, MAX_HEIGHT, MAX_WIDTH } from '../../consts/sizes';
import { gameStateUrl, userActionUrl, gameRestartUrl } from '../../consts/urls';
import errorHandler from "../../utils/errorHandler";
import Instruction from "../Instruction";

import './base.css'

export default class App extends React.Component {
    constructor() {
        super();
        this.state = {
            people: [],
            map: [],
            instructionOpen: true,
        }
        this.intervalId = null;
    }

    componentWillUnmount() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
        }
    }

    render() {
        const { people, map, instructionOpen } = this.state;
        return (
            <div className={ styles.root }>
                { instructionOpen && <Instruction onClose={ this.closeInstruction }/> }
                <div className={styles.container}>
                    <h1 className={ styles.title }>Симулятор COVID</h1>
                    <button onClick={ this.restartGame } className={styles.button}>Начать сначала</button>
                </div>
                <Field
                    map={ map }
                    people={ people }
                    onClick={ this.personClick }/>

            </div>
        );
    }

    restartGame = () => {
        const response = fetch(gameRestartUrl);
        this.setGameStateFromServer(response);
    };

    setGameStateFromServer = (promise) => {
        promise.then(errorHandler)
            .then(res => res.json())
            .then(game => {
                this.setState({
                    people: game.people,
                    map: game.map.houses.map(i => ({
                        x: i.coordinates.leftTopCorner.x,
                        y: i.coordinates.leftTopCorner.y,
                        isMarket: i.isMarket
                    })),
                })
            })
    };

    closeInstruction = () => {
        this.setState({
            instructionOpen: false,
        })

        this.getNewStateFromServer();

        this.intervalId = setInterval(this.getNewStateFromServer, DELAY);
    }

    personClick = (id) => {
        fetch(userActionUrl, {
            method: 'POST',
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                personClicked: id,
            }),
        }).then(errorHandler)
    }

    getNewStateFromServer = () => {
        const response = fetch(gameStateUrl);
        this.setGameStateFromServer(response);
    }
}